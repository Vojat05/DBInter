namespace DBInter
{
    internal class Program
    {
        private static string? PATH;
        private static string? DATABASE = "";
        private static string? TABLE;

        // Printing command help
        static void printHelp()
        {
            Console.WriteLine(@"Command help:
-> connect [db file]                                   | connect Test.db
-> clear   [db table]                                  | clear data
-> create  [db table (column data column data)]        | create data (id INTEGER PRIMARY KEY, name TEXT, price INTEGER)
-> write   [db table (column, column) (value, value)]  | write data (name, price) ('graphics card', 2500)
-> delete  [db table condition]                        | delete data id = 2
-> remove  [db table]                                  | remove data
-> raw     [command]                                   | raw (DELETE FROM data WHERE id=1;)
-> print   [db table selector]                         | print data *
-> print columns [db table]                            | print columns data
-> print tables
-> help
-> ls
-> cd      [db table]                                  | cd data
-> exit or quit");
        }
        static void Main(string[] args)
        {
            Console.WriteLine(@"
____________  ___  ___                                  
|  _  \ ___ \ |  \/  |                                  
| | | | |_/ / | .  . | __ _ _ __   __ _  __ _  ___ _ __ 
| | | | ___ \ | |\/| |/ _` | '_ \ / _` |/ _` |/ _ \ '__|
| |/ /| |_/ / | |  | | (_| | | | | (_| | (_| |  __/ |   
|___/ \____/  \_|  |_/\__,_|_| |_|\__,_|\__, |\___|_|   
                                   __/ |          
                                  |___/           
");
            Console.WriteLine("Type \"help\" to show possible commands");
            Console.WriteLine();

            DBManager dbManager = new DBManager();


        START:
            Console.Write($"{PATH}{(PATH == null || PATH.Length == 0 ? null : ' ')}> ");
            Console.ForegroundColor = ConsoleColor.Cyan;

            string input = "" + Console.ReadLine();
            Console.ResetColor();
            List<string> command = new List<string>();
            string com = "";
            bool inArg = false;

            // Parse the input string to get the command and it's arguments
            for (int i=0; i<input.Length; i++)
            {
                if (input[i] == '(') inArg = true;
                else if (input[i] == ')') inArg = false;
                if (input[i] == ' ' && !inArg)
                {
                    command.Add(com);
                    com = "";
                    continue;
                }
                com += input[i];
            }
            command.Add(com);



            /*
             * |------------------------------------------------|
             * |---------|     Command execution     |----------|
             * |------------------------------------------------|
             */
            switch (command[0])
            {
                case "connect":
                    int i = command[1].Length - 1;
                    while (i >= 0)
                    {
                        // Try to get only the database name, not the entire path specified
                        if (command[1][i] != '/')
                        {
                            // Still in the database name portion
                            DATABASE = command[1][i--] + DATABASE;
                        }
                        else break;
                    }
                    Console.WriteLine($"Connecting to database {DATABASE}!");
                    dbManager.createConnection(command[1]);
                    dbManager.openConnection();
                    dbManager.fillTableNames();
                    Console.WriteLine($"Connection to {DATABASE} open!");
                    PATH += DATABASE;
                    goto START;

                case "cd":
                    if (command.Count < 2) Console.WriteLine("Not enough arguments, you must specify the database table to connect to!");
                    if (DBManager.tableNames.Contains(command[1]))
                    {
                        TABLE = command[1];
                        PATH = $"{DATABASE}/{TABLE}";
                    }
                    else if (command[1].Equals(".."))
                    {
                        TABLE = null;
                        PATH = $"{DATABASE}";
                    }
                    else Console.WriteLine($"Database doesn't contain table called {command[1]}!");
                    goto START;

                case "ls":
                    if (DATABASE == null) Console.WriteLine("No database connection!\nPlease use the connect command.");
                    else if (TABLE == null) dbManager.printTables();
                    else dbManager.printSelection(dbManager.readList(TABLE, "*"));
                    goto START;

                case "clear":
                    if (TABLE != null)
                    {
                        dbManager.clear(TABLE);
                        Console.WriteLine($"Database table {TABLE} cleared!");
                    } else
                    {
                        dbManager.clear(command[1]);
                        Console.WriteLine($"Database table {command[1]} cleared!");
                    }
                    goto START;

                case "create":
                    Console.WriteLine($"Creating table {command[1]}!");
                    dbManager.createTable(command[1], command[2]);
                    Console.WriteLine($"Table {command[1]} created!");
                    goto START;

                case "write":
                    try
                    {
                        if (TABLE != null && command.Count < 4)
                        {
                            Console.WriteLine($"Writing into table {TABLE}!");
                            dbManager.write(TABLE, command[1], command[2]);
                            Console.WriteLine($"Writing into table {TABLE} complete!");
                        }
                        else
                        {
                            Console.WriteLine($"Writing into table {command[1]}!");
                            dbManager.write(command[1], command[2], command[3]);
                            Console.WriteLine($"Writing into {command[1]} complete!");
                        }
                    } catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message);
                        Console.ResetColor();
                    }
                    goto START;

                case "help":
                    printHelp();
                    goto START;

                case "exit": case "quit":
                    goto EXIT;

                case "print":
                    if (command.Count == 3 && command[1].Equals("columns"))
                    {
                        dbManager.printColumns(command[2]);
                        goto START;
                    } else if (command.Count == 2 && command[1].Equals("tables"))
                    {
                        dbManager.printTables();
                        goto START;
                    } else if (command.Count <= 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Not enough arguments, you must specify database table and selector!");
                        Console.ResetColor();
                        goto START;
                    }
                    dbManager.printSelection(dbManager.readList(command[1], command[2]));
                    goto START;

                case "delete":
                    if (command.Count < 3)
                    {
                        Console.WriteLine("Not enough arguments, you must specify database table and delete condition!");
                        goto START;
                    }
                    dbManager.delete(command[1], command[2]);
                    Console.WriteLine("Database entry deleted!");
                    goto START;

                case "remove":
                    if (command.Count < 2)
                    {
                        Console.WriteLine("Not enough arguments, you must specify database table to be dropped!");
                        goto START;
                    }
                    try
                    {
                        dbManager.dropTable(command[1]);
                        Console.WriteLine($"Database table {command[1]} removed!");
                    } catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message);
                        Console.ResetColor();
                    }
                    goto START;

                case "raw":
                    dbManager.exec(command[1]);
                    Console.WriteLine("Command has been executed!");
                    goto START;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Command doesn't exist\n");
                    Console.ResetColor();
                    goto START;
            }

        EXIT:
            dbManager.close();
            return;
        }
    }
}