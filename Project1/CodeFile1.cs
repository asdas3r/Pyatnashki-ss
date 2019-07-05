using System;
using System.Linq;
using System.Collections.Generic;

class InitProblem
{
    public byte[,] initState = new byte[,] { { 5, 8, 3 }, { 2, 0, 4 }, { 7, 6, 1 } };
    //public byte[,] initState = new byte[,] { { 1, 3, 4 }, {8, 2, 0 }, { 7, 6, 5 } };
    public sbyte[,] operations = new sbyte[,] { { -1, 0 }, { 0, 1 }, { 1, 0 }, { 0, -1 } };
    public byte[,] goalState = new byte[,] { { 1, 2, 3 }, { 8, 0, 4 }, { 7, 6, 5 } };

    public bool GoalTest(byte[,] state)
    {
        if (goalState.Cast<byte>().SequenceEqual(state.Cast<byte>()))
            return true;
        return false;
    }
}

class Node
{
    public Node(byte[,] initState)
    {
        state = (byte[,])initState.Clone();
    }

    private Node pr_parentNode = null;
    public byte[,] state = new byte[3, 3];
    public sbyte[] action = new sbyte[2];
    public Node parentNode
    {
        get
        {
            return pr_parentNode;
        }
    }
    public int depth;
    public int pathCost;

    public Node AddChild(sbyte[] operation)
    {
        Node child = new Node(state);
        child.pr_parentNode = this;
        child.depth = depth + 1;
        child.action = operation;
        MoveTile(child, operation);
        return child;
    }

    public void SetPathCost(int cost)
    {
        pathCost = cost;
    }

    public void MoveTile(Node node, sbyte[] operation)
    {
        byte[] etc = FindTileCoords(node.state, 0);
        byte i, j;
        i = (byte)(etc[0] + operation[0]);
        j = (byte)(etc[1] + operation[1]);
        SwapTiles(node, etc[0], etc[1], i, j);
    }

    public void SwapTiles(Node node, byte i1, byte j1, byte i2, byte j2)
    {
        byte buf = node.state[i1, j1];
        node.state[i1, j1] = node.state[i2, j2];
        node.state[i2, j2] = buf;
    }

    public static byte[] FindTileCoords(byte[,] someState, byte coord)
    {
        byte[] returnCoords = { 255, 255 };
        for (byte i = 0; i < someState.GetLength(0); i++)
        {
            for (byte j = 0; j < someState.GetLength(1); j++)
            {
                if (someState[i, j] == coord)
                {
                    returnCoords[0] = i;
                    returnCoords[1] = j;
                    return returnCoords;
                }
            }
        }
        if (returnCoords[0] == 255)
            throw new Exception("Tile with that number not found");
        return returnCoords;
    }
}

class GeneralSearch
{
    List<Node> nodesQ;
    InitProblem problem;
    HashSet<String> nodesHashSet;
    int nodesOpened;
    int nodesQueued;
    int maxQueue;

    public GeneralSearch(InitProblem outerProblem)
    {
        nodesQ = new List<Node>();
        nodesHashSet = new HashSet<String>();
        problem = new InitProblem();
    }

    public Node MakeNode(byte[,] state)
    {
        return new Node(state);
    }

    public void MakeQueue(Node nodeA)
    {
        nodesQ.Add(nodeA);
    }

    public bool IsEmpty()
    {
        if (nodesQ.Count == 0)
            return true;
        return false;
    }

    public Node RemoveFront()
    {
        Node frontNode = nodesQ[0];
        nodesQ.RemoveAt(0);
        return frontNode;
    }

    public Node[] Expand(Node rootNode, AbstractSearch searchMethod)
    {
        sbyte iDim, jDim;
        byte[] etc = Node.FindTileCoords(rootNode.state, 0);
        List<Node> nodesList = new List<Node>();
        for (int x = 0; x < problem.operations.GetLength(0); x++)
        {
            iDim = (sbyte)(etc[0] + problem.operations[x, 0]);
            jDim = (sbyte)(etc[1] + problem.operations[x, 1]);
            if ((iDim < 0) || (iDim > rootNode.state.GetLength(0) - 1))
                continue;
            if ((jDim < 0) || (jDim > rootNode.state.GetLength(1) - 1))
                continue;
            sbyte[] operatorsX = new sbyte[2];
            operatorsX[0] = problem.operations[x, 0];
            operatorsX[1] = problem.operations[x, 1];
            Node childNode = rootNode.AddChild(operatorsX);
            childNode.SetPathCost(searchMethod.PathCostFn(childNode));
            if (rootNode.parentNode == null || !(childNode.state.Cast<byte>().SequenceEqual(rootNode.parentNode.state.Cast<byte>())))
            {
                if (nodesHashSet.Contains(String.Join("", childNode.state.Cast<byte>())) == false)
                {
                    nodesList.Add(childNode);
                    nodesHashSet.Add(String.Join("", childNode.state.Cast<byte>()));
                    nodesQueued++;
                }
            }
        }
        if (nodesList.Count != 0)
            nodesOpened++;
        return nodesList.ToArray();
    }

    public void Program(AbstractSearch searchMethod)
    {
        int n = 0;
        long prevIterSec = 0;
        System.Diagnostics.Stopwatch watch;
        watch = System.Diagnostics.Stopwatch.StartNew();
        if (!LabAIMain.stepsYN)
            Console.Write("\nВыполняется работа алгоритма...");
        MakeQueue(MakeNode(problem.initState));
        while (true)
        {
            if (LabAIMain.stepsYN)
            {
                Console.BackgroundColor = ConsoleColor.Magenta;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[ШАГ #{0}]", ++n);
                Console.ResetColor();
            }
            if (IsEmpty())
            {
                Console.WriteLine("\n\nРешений не было найдено.\n");
                return;
            }
            Node newNode = RemoveFront();
            if (problem.GoalTest(newNode.state))
            {
                watch.Stop();
                Console.WriteLine("\n\nРешение было найдено!\n");
                ShowSolution(newNode);
                Console.WriteLine("\nВремя работы алгоритма: {0}.{1:000} секунд(ы)", watch.ElapsedMilliseconds / 1000, watch.ElapsedMilliseconds % 1000);
                Console.WriteLine("Вершин открыто: {0}", nodesOpened);
                Console.WriteLine("Вершин поставлено в очередь: {0}", nodesQueued);
                Console.WriteLine("Максимальная длина очереди: {0}\n", maxQueue < nodesQ.Count ? maxQueue = nodesQ.Count : maxQueue);
                return;
            }
            Node[] nodes = Expand(newNode, searchMethod);
            searchMethod.QueueingFn(nodesQ, nodes);
            if ((watch.ElapsedMilliseconds / 1000 != prevIterSec) && !LabAIMain.stepsYN)
                Console.Write(".");
            prevIterSec = watch.ElapsedMilliseconds / 1000;
            if (LabAIMain.stepsYN)
            {
                Console.WriteLine("depth - {0}, cost - {1}", newNode.depth, newNode.pathCost);
                ShowNode(newNode);
                Console.WriteLine("Перемещение пустой фишки - " + MoveDirection(newNode.action));
                Console.WriteLine("\nРаскрытые вершины:");
                ShowNodesArray(nodes);
                Console.WriteLine("\nОжидают раскрытия:");
                ShowNodesArray(nodesQ.ToArray());
                Console.Write("\n...Любая клавиша - следующий шаг / 'Escape' - выход из пошагового режима...");
                ConsoleKey pressedKey = Console.ReadKey(true).Key;
                Console.SetCursorPosition(0, Console.CursorTop);
                for (int i = 0; i < 80; i++)
                    Console.Write(" ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                if (pressedKey == ConsoleKey.Escape)
                {
                    Console.Write("\nВыход из пошагового режима. Выполняется работа алгоритма...");
                    LabAIMain.stepsYN = false;
                }
                    
            }
        }
    }

    public void ShowNode(Node finalNode)
    {
        for (int i = 0; i < finalNode.state.GetLength(0); i++)
        {
            Console.Write("     ");
            for (int j = 0; j < finalNode.state.GetLength(1); j++)
            {
                Console.Write("{0}  ", finalNode.state[i, j]);
            }
            Console.WriteLine();
        }
    }

    public void ShowNodesArray(Node[] nodes)
    {
        int amount = 6;
        if (nodes.Length == 0)
        {
            Console.WriteLine("Вершины отсутствуют");
            return;
        }
        for (int i = 0; i < nodes[0].state.GetLength(0); i++)
        {
            for (int n = 0; (n < nodes.Length) && (n < amount); n++)
            {
                Console.Write("     ");
                for (int j = 0; j < nodes[n].state.GetLength(1); j++)
                {
                    Console.Write("{0}  ", nodes[n].state[i, j]);
                }
                if ((nodes.Length > amount) && (i == (nodes[n].state.GetLength(0) - 1) / 2) && ((nodes[n].state.GetLength(0) - 1) % 2 == 0) && (n == amount - 1)) {
                    Console.Write("   ...и еще {0} вершин...", nodes.Length - amount);
                }
            }
            Console.WriteLine();
        }
    }

    public void ShowSolution(Node finalNode)
    {
        Node mover = finalNode;
        List<Node> nodeList = new List<Node>();
        do
        {
            nodeList.Add(mover);
            mover = mover.parentNode;
        } while (mover != null);
        nodeList.Reverse();
        Console.WriteLine("1) Начальная вершина:");
        ShowNode(nodeList[0]);
        for (int i = 1; i < nodeList.Count; i++)
        {
            Console.WriteLine(i + 1 + ") Перемещение пустой фишки - " + MoveDirection(nodeList[i].action) + ": ");
            ShowNode(nodeList[i]);
        }
        Console.WriteLine();
    }

    public String MoveDirection(sbyte[] move)
    {   
        if (move[0] == 1)
            return "Вниз (\u2193)";
        if (move[0] == -1)
            return "Вверх (\u2191)";
        if (move[1] == 1)
            return "Вправо (\u2192)";
        if (move[1] == -1)
            return "Влево (\u2190)";
        return "нет";
    }
}

abstract class AbstractSearch
{
    abstract public void QueueingFn(List<Node> nodeq, Node[] nodesArray);
    abstract public int PathCostFn(Node node);
}

class BreadthFirstSearch : AbstractSearch
{
    override public void QueueingFn(List<Node> nodeq, Node[] nodesArray)
    {

        for (int i = 0; i < nodesArray.Length; i++)
        {
            nodeq.Add(nodesArray[i]);
        }
    }

    override public int PathCostFn(Node node)
    {
        return node.depth;
    }
}

class BreadthFirstCostSearch : AbstractSearch
{
    override public void QueueingFn(List<Node> nodeq, Node[] nodesArray)
    {
        int left, right, mid;
        bool inserted;
        for (int i = 0; i < nodesArray.Length; i++)
        {
            inserted = false;
            left = 0;
            right = nodeq.Count-1;
            while (left < right - 1)
            {
                mid = (left + right) / 2;
                if ((nodesArray[i].pathCost <= nodeq[mid - 1].pathCost) && (nodesArray[i].pathCost <= nodeq[mid].pathCost))
                {
                    right = mid;
                    continue;
                }
                if ((nodesArray[i].pathCost > nodeq[mid - 1].pathCost) && (nodesArray[i].pathCost > nodeq[mid].pathCost))
                {
                    left = mid;
                    continue;
                }
                nodeq.Insert(mid, nodesArray[i]);
                inserted = true;
                break;
            }
            if (!inserted)
            {
                if ((left == right) && (nodesArray[i].pathCost <= nodeq[left].pathCost))
                {
                    nodeq.Insert(left, nodesArray[i]);
                }
                else if (right == left + 1)
                {
                    if (nodesArray[i].pathCost <= nodeq[right].pathCost)
                    {
                        if (nodesArray[i].pathCost <= nodeq[left].pathCost)
                        {
                            nodeq.Insert(left, nodesArray[i]);
                        }
                        else
                        {
                            nodeq.Insert(right, nodesArray[i]);
                        }
                    }
                    else
                    {
                        nodeq.Add(nodesArray[i]);
                    }
                }
                else
                {
                    nodeq.Add(nodesArray[i]);
                }
            }
        }
    }

    override public int PathCostFn(Node node)
    {
        byte[] etc = Node.FindTileCoords(node.state, 0);
        int n = node.state[etc[0] - node.action[0], etc[1] - node.action[1]];
        if (node.parentNode != null)
        {
            n += node.parentNode.pathCost;
        }
        return n;
    }
}

interface iEvalMethod
{
    int Function(Node node);
}

class BFS : AbstractSearch
{
    iEvalMethod EvalFn;

    public BFS(iEvalMethod funct)
    {
        EvalFn = funct;
    }

    override public void QueueingFn(List<Node> nodeq, Node[] nodesArray)
    {
        int left, right, mid;
        bool inserted;
        for (int i = 0; i < nodesArray.Length; i++)
        {
            inserted = false;
            left = 0;
            right = nodeq.Count - 1;
            while (left < right - 1)
            {
                mid = (left + right) / 2;
                if ((nodesArray[i].pathCost <= nodeq[mid - 1].pathCost) && (nodesArray[i].pathCost <= nodeq[mid].pathCost))
                {
                    right = mid;
                    continue;
                }
                if ((nodesArray[i].pathCost > nodeq[mid - 1].pathCost) && (nodesArray[i].pathCost > nodeq[mid].pathCost))
                {
                    left = mid;
                    continue;
                }
                nodeq.Insert(mid, nodesArray[i]);
                inserted = true;
                break;
            }
            if (!inserted)
            {
                if ((left == right) && (nodesArray[i].pathCost <= nodeq[left].pathCost))
                {
                    nodeq.Insert(left, nodesArray[i]);
                }
                else if (right == left + 1)
                {
                    if (nodesArray[i].pathCost <= nodeq[right].pathCost)
                    {
                        if (nodesArray[i].pathCost <= nodeq[left].pathCost)
                        {
                            nodeq.Insert(left, nodesArray[i]);
                        }
                        else
                        {
                            nodeq.Insert(right, nodesArray[i]);
                        }
                    }
                    else
                    {
                        nodeq.Add(nodesArray[i]);
                    }
                }
                else
                {
                    nodeq.Add(nodesArray[i]);
                }
            }
        }
    }

    override public int PathCostFn(Node node)
    {
        return EvalFn.Function(node);
    }
}

class WrongPlacesSearchFunction : iEvalMethod
{
    public int Function(Node node)
    {
        int f = 0;
        InitProblem problem = new InitProblem();
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < 3; j++)
            {
                if (node.state[i,j] != problem.goalState[i, j])
                    f++;
            }
        }
        if (node.parentNode != null)
        {
            f += node.depth;
        }
        return f;
    }
}

class ManhattanSearchFunction : iEvalMethod
{
    public int Function(Node node)
    {
        int f = 0;
        InitProblem problem = new InitProblem();
        byte[] tileCoords = new byte[2];
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < 3; j++)
            {
                tileCoords = Node.FindTileCoords(problem.goalState, node.state[i,j]);
                f += Math.Abs(i - tileCoords[0]) + Math.Abs(j - tileCoords[1]);
            }
        }
        if (node.parentNode != null)
        {
            f += node.depth;
        }
        return f;
    }
}

class LabAIMain
{
    public static bool stepsYN;

    public static void Main()
    {
        String[] mainMenuLabels =
        {
            "[МЕНЮ ПРОГРАММЫ]",
            "1. Поиск в ширину",
            "2. Поиск по критерию стоимости",
            "3. Поиск эвристики неверных позиций",
            "4. Поиск эвристики Манхэттенских состояний",
            "Выйти из программы"
        };

        String[] ynStepsMenuLabels =
        {
            "Запустить в пошаговом режиме?",
            "Да",
            "Нет"
        };

        while (true)
        {
            InitProblem Problem = new InitProblem();
            GeneralSearch test = new GeneralSearch(Problem);
            Console.Clear();
            InteractiveMenuClass mainMenu = new InteractiveMenuClass(mainMenuLabels);
            int exeSearchVariant = mainMenu.Start();

            Console.WriteLine();
            InteractiveMenuClass ynStepsMenu = new InteractiveMenuClass(ynStepsMenuLabels);

            switch (exeSearchVariant)
            {
                case 1:
                    {
                        stepsYN = ynStepsMenu.Start() == 1;
                        test.Program(new BreadthFirstSearch());
                        break;
                    }
                case 2:
                    {
                        stepsYN = ynStepsMenu.Start() == 1;
                        test.Program(new BreadthFirstCostSearch());
                        break;
                    }
                case 3:
                    {
                        stepsYN = ynStepsMenu.Start() == 1;
                        test.Program(new BFS(new WrongPlacesSearchFunction()));
                        break;
                    }
                case 4:
                    {
                        stepsYN = ynStepsMenu.Start() == 1;
                        test.Program(new BFS(new ManhattanSearchFunction()));
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
            Console.Write("Нажмите любую кнопку чтобы перейти в меню...");
            Console.ReadKey(true);
        }
    }
}

class InteractiveMenuClass
{
    String[] menuItems;
    int itemsAmount;

    public InteractiveMenuClass(String[] items)
    {
        itemsAmount = items.Length;
        menuItems = new String[itemsAmount];
        for (int i = 0; i < itemsAmount; i++)
        {
            menuItems[i] = String.Copy(items[i]);
        }
    }

    public int InteractiveMenuControl()
    {
        int n = 1;
        InteractiveMenuInterface(n);
        ConsoleKey pressedKey;
        do
        {
            pressedKey = Console.ReadKey().Key;
            switch (pressedKey)
            {
                case (ConsoleKey.DownArrow):
                    {
                        n++;
                        if (n > itemsAmount - 1) n = 1;
                        InteractiveMenuInterface(n);
                        break;
                    }
                case (ConsoleKey.UpArrow):
                    {
                        n--;
                        if (n < 1) n = itemsAmount - 1;
                        InteractiveMenuInterface(n);
                        break;
                    }
            }
        } while (pressedKey != ConsoleKey.Enter);
        return n;
    }

    public void InteractiveMenuInterface(int n)
    {
        if ((Console.CursorTop - itemsAmount) < 0)
            Console.SetCursorPosition(0, 0);
        else
            Console.SetCursorPosition(0, Console.CursorTop - itemsAmount);
        Console.WriteLine(menuItems[0]);
        for (int i = 1; i < itemsAmount; i++)
            {
                if (i == n)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine(menuItems[i]);
                Console.ResetColor();
            }
    }

    public int Start()
    {
        for (int i = 0; i < itemsAmount; i++)
            Console.WriteLine();
        return InteractiveMenuControl();
    }
}