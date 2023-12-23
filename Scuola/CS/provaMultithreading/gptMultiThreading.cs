using System;
using System.Collections.Generic;
using System.Threading;

class SquareNull : Character
{
    public SquareNull() : base()
    {
        isSquareOccupied = false;
    }
}

class Character
{
    public char Value { get; set; }
    public List<Tuple<int, int>> ValidMoves { get; set; }
    public bool isSquareOccupied { get; set; }
    public int Life { get; set; } = 5;

    public Character(char value)
    {
        Value = value;
        ValidMoves = new List<Tuple<int, int>>();
        isSquareOccupied = false;
    }

    public Character() { Value = '*'; }
}

class CharMatrix
{
    private Character[,] matrix;
    private Random random;
    private int matrixSize;

    private bool gameIsRunning = false;
    private Character Winner = null;
    private HashSet<Character> chosenCharacters = new HashSet<Character>();

    private ManualResetEvent stopEvent = new ManualResetEvent(false);

    public CharMatrix(int size)
    {
        matrixSize = size;

        matrix = new Character[matrixSize, matrixSize];
        random = new Random();

        gameIsRunning = true;
        StartGame();
    }

    private void StartGame()
    {
        FillWithRandomCharacters();

        for (int i = 0; i < 3; i++)
        {
            // Create threads and start them
            Thread thread = new Thread(MoveRandomCharacter);
            thread.Start();
            thread.Name = "Thread " + i.ToString();
        }
        Thread printThread = new Thread(Printing), checkForWinnerThread = new Thread(CheckForWinner);
        printThread.Start();
        checkForWinnerThread.Start();

        // Keep the main thread alive until all other threads complete
        stopEvent.WaitOne();
    }

    private void Printing()
    {
        while (gameIsRunning)
        {
            PrintMatrix();
            Thread.Sleep(10);
        }
    }

    private void CheckForWinner()
    {
        while (gameIsRunning)
        {
            if (CheckWinner() <= 1)
            {
                gameIsRunning = false;
                break;
            }
            Thread.Sleep(1000);
        }

        stopEvent.Set();
    }

    private void MoveRandomCharacter()
    {
        while (gameIsRunning)
        {
            int x, y;

            Monitor.Enter(this);

            x = random.Next(0, matrixSize);
            y = random.Next(0, matrixSize);

            if (chosenCharacters.Contains(matrix[x, y]))
            {
                Monitor.Wait(this);
            }

            chosenCharacters.Add(matrix[x, y]);

            MoveCharacter(x, y);

            //Monitor.Enter(this);
            if (chosenCharacters.Count() > 0)
                chosenCharacters.Remove(matrix[x, y]);

            Monitor.Pulse(this);

            Monitor.Exit(this);
        }
    }

    private void MoveToCell(int previousX, int previousY, int destX, int destY)
    {
        Thread.Sleep(5);

        if (matrix[previousX, previousY].isSquareOccupied)
            return;

        Character character = matrix[previousX, previousY];

        if (matrix[destX, destY] is SquareNull)
        {
            SquareNull squareNull = (SquareNull)matrix[destX, destY];
            matrix[previousX, previousY] = squareNull;
        }
        else
            matrix[previousX, previousY] = new SquareNull();
        
        matrix[destX, destY] = character;

        matrix[destX, destY].Life--;

        matrix[previousX, previousY].isSquareOccupied = false;
    }

    private void LockSquare(int previousX, int previousY, int destX, int destY)
    {
        Monitor.Enter(this);

        if (matrix[destX, destY].isSquareOccupied)
        {
            Monitor.Wait(this);
        }
        
        matrix[destX, destY].isSquareOccupied = true;

        MoveToCell(previousX, previousY, destX, destY);

        Monitor.Pulse(this);

        Monitor.Exit(this);

        Thread.Sleep(5);
    }

    private void MoveCharacter(int x, int y)
    {
        if (matrix[x, y] is SquareNull)
            return;

        CalculateValidMoves(x, y);
        RemoveInvalidMoves(x, y);

        if (matrix[x, y].ValidMoves.Count < 1)
            return;

        int previousX = x, previousY = y;

        Tuple<int, int> destination = RandomMove(x, y);

        LockSquare(previousX, previousY, destination.Item1, destination.Item2);

        Thread.Sleep(1000);

        /*if (character.Life <= 0)
        {
            Monitor.Enter(this);

            try
            {
                matrix[destination.Item1, destination.Item2].Value = (char)(matrix[destination.Item1, destination.Item2].Value - 32);
                PrintMatrix();
                Thread.Sleep(1500);
                matrix[destination.Item1, destination.Item2] = new SquareNull();
            }
            finally
            {
                Monitor.Exit(this);
            }
        }*/
    }

    private Tuple<int, int> RandomMove(int x, int y)
    {
        int validMovesLength = matrix[x, y].ValidMoves.Count();

        return matrix[x, y].ValidMoves[random.Next(0, validMovesLength)];
    }

    private int CheckWinner()
    {
        int counter = 0;

        Character possibleWinner = null;

        for (int i = 0; i < matrixSize; i++)
        {
            for (int j = 0; j < matrixSize; j++)
            {
                if (!(matrix[i, j] is SquareNull))
                {
                    possibleWinner = matrix[i, j];
                    counter++;
                }

                if (counter > 1)
                    return counter;
            }
        }

        if (possibleWinner == null)
            return 0;

        Winner = possibleWinner;

        return 1;
    }

    public void FillWithRandomCharacters()
    {
        int i, j;

        for (i = 0; i < matrix.GetLength(0); i++)
        {
            for (j = 0; j < matrix.GetLength(1); j++)
            {
                matrix[i, j] = new Character(GetRandomChar());
            }
        }

        i = random.Next(0, matrixSize);
        j = random.Next(0, matrixSize);

        matrix[i, j] = new SquareNull();
    }

    private char GetRandomChar()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return chars[random.Next(chars.Length)];
    }

    public void PrintMatrix()
    {
        Console.SetCursorPosition(0, 4);

        for (int i = 0; i < matrixSize; i++)
        {
            for (int j = 0; j < matrixSize; j++)
            {
                Console.Write(matrix[i, j].Value + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    public void CalculateValidMoves(int row, int col)
    {
        ref Character character = ref matrix[row, col];
        character.ValidMoves.Clear();

        character.ValidMoves.Add(Tuple.Create(row - 1, col));
        character.ValidMoves.Add(Tuple.Create(row + 1, col));
        character.ValidMoves.Add(Tuple.Create(row, col - 1));
        character.ValidMoves.Add(Tuple.Create(row, col + 1));
    }

    public void RemoveInvalidMoves(int row, int col)
    {
        Character character = matrix[row, col];

        character.ValidMoves.RemoveAll(move => !IsValidCell(move.Item1, move.Item2));
        //character.ValidMoves.RemoveAll(move => !CanEat(character, matrix[move.Item1, move.Item2]));
    }

    private bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < matrixSize && col >= 0 && col < matrixSize;
    }

    private bool CanEat(Character source, Character target)
    {
        return !(target is SquareNull) && source.Value - target.Value == 1;
    }
}

class Program
{
    static void Main()
    {
        CharMatrix charMatrix = new CharMatrix(3);
    }
}
