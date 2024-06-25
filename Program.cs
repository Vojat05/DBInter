namespace DBInter
{
    internal class Program
    {
        private static string? PATH;
        private static string? DATABASE;
        private static string? TABLE;
        private static DBManager dbManager;

        // Printing command help
        static void printHelp()
        {
            Console.WriteLine(@"
-------------------------------------------------------|  Command help  |-------------------------------------------------------

Command                 Arguments                                     Example
--------------------------------------------------------------------------------------------------------------------------------
-> connect              | [db file]                                   | connect Test.db
-> clear | clr | cls    | [db table]                                  | clear data
-> create               | [db table (column data column data)]        | create data (id INTEGER PRIMARY KEY, name TEXT, price INTEGER)
-> write                | [db table (column, column) (value, value)]  | write data (name, price) ('graphics card', 2500)
-> delete               | [db table condition]                        | delete data id=2
-> remove               | [db table]                                  | remove data
-> raw                  | [command]                                   | raw (DELETE FROM data WHERE id=1;)
-> print                | [selector dbTable]                          | print * data
-> print columns        | [db table]                                  | print columns data
-> cd                   | [db table]                                  | cd data
-> print tables
-> help
-> ls
-> cwd
-> exit or quit");
        }

        static void error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        static int count(string message, char c)
        {
            int count = 0;

            for (int i = 0; i < message.Length; i++)
                if (message[i] == c) count++;

            return count;
        }

        static bool isNumber(string message)
        {
            for (int i = 0; i < message.Length; i++)
                if (message[i] < 48 || message[i] > 57) return false;
            return true;
        }

        static void Main(string[] args)
        {
            Console.SetWindowSize(150, 40);
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

            if (args.Length == 0) dbManager = new DBManager();
            else 
            {
                dbManager = new DBManager(args[0]);
                DATABASE = args[0];
                PATH = args[0];
            }

        START:
            Console.Write($"{PATH}{(PATH == null || PATH.Length == 0 ? null : ' ')}> ");
            Console.ForegroundColor = ConsoleColor.Cyan;

            string input = "" + Console.ReadLine();
            Console.ResetColor();
            List<string> command = new List<string>();
            string com = "";
            bool inArg = false;
            bool inString = false;

            // Parse the input string to get the command and it's arguments
            for (int i=0; i<input.Length; i++)
            {
                if (input[i] == '(') inArg = true;
                else if (input[i] == ')') inArg = false;
                else if (input[i] == '\'') inString = inString ? false : true;
                if (input[i] == ' ' && !inArg && !inString)
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
                    PATH = DATABASE;
                    goto START;

                // Change directory
                case "cd":
                    if (command.Count < 2) error("Not enough arguments, you must specify the database table to connect to!");
                    if (dbManager.tableNames.Contains(command[1]))
                    {
                        TABLE = command[1];
                        PATH = $"{DATABASE}/{TABLE}";
                    }
                    else if (command[1].Equals(".."))
                    {
                        TABLE = null;
                        PATH = $"{DATABASE}";
                    }
                    else error($"Database doesn't contain table called {command[1]}!");
                    goto START;

                // Print the current directory contents
                case "ls":
                    if (DATABASE == null) error("No database connection!\nPlease use the connect command.");
                    else if (TABLE == null) dbManager.printTables();
                    else dbManager.printSelection(dbManager.readList(TABLE, "*"));
                    goto START;

                // Prints the current working directory in operating system;
                case "cwd":
                    Console.WriteLine(Directory.GetCurrentDirectory());
                    goto START;

                // Clear the database table or screen
                case "clear":
                case "clr":
                case "cls":
                    if (TABLE == null && command.Count > 1)
                    {
                        dbManager.clear(command[1]);
                        Console.WriteLine($"Database table {command[1]} cleared!");
                    } else if (command.Count > 1 && TABLE != null)
                    {
                        dbManager.clear(TABLE);
                        Console.WriteLine($"Database table {TABLE} cleared!");
                    } else Console.Clear();
                    goto START;

                // Create a database table
                case "create":
                    if (command.Count != 3) error("You must specify exactly 2 arguments [table] [(data)]");
                    else
                    {
                        Console.WriteLine($"Creating table {command[1]}!");
                        dbManager.createTable(command[1], command[2]);
                        Console.WriteLine($"Table {command[1]} created!");
                    }
                    goto START;

                // Write into a database table
                case "write":
                    try
                    {
                        if (TABLE != null && command.Count == 3)
                        {
                            Console.WriteLine($"Writing into table {TABLE}!");
                            dbManager.write(TABLE, command[1], command[2]);
                            Console.WriteLine($"Writing into table {TABLE} complete!");
                        }
                        else if (command.Count == 4)
                        {
                            Console.WriteLine($"Writing into table {command[1]}!");
                            dbManager.write(command[1], command[2], command[3]);
                            Console.WriteLine($"Writing into {command[1]} complete!");
                        } else if (TABLE != null && command.Count == 2)
                        {
                            while (true)
                            {
                                string[] columns = new string[count(command[1], ',') + 1];
                                string[] values = new string[count(command[1], ',') + 1];

                                // Fill the columns array with column names
                                string column = "";
                                int indexer = 0;
                                for (i = 0; i < command[1].Length; i++)
                                {
                                    switch (command[1][i])
                                    {
                                        case '(':
                                        case '"':
                                        case '\'': break;

                                        case ')':
                                        case ',':
                                            columns[indexer++] = column;
                                            column = "";
                                            break;

                                        default: 
                                            column += command[1][i];
                                            break;
                                    }
                                }
                                Console.WriteLine("---------------------------------------------------------------------------------");
                                // Reading values
                                for (i = 0; i < columns.Length; i++)
                                {
                                    string value;
                                    Console.Write((columns[i][0] == ' ' ? columns[i].Substring(1) : columns[i]) + " >> ");
                                    value = Console.ReadLine();
                                    if (value.Equals("$exit")) goto START;
                                    values[i] = value;
                                }

                                // Writing into database
                                string fields = "(";
                                string data = "(";

                                for (i = 0; i < columns.Length; i++)
                                    fields += (i == 0 ? "" : ",") + columns[i];
                                fields += ")";

                                for (i = 0; i < values.Length; i++)
                                {
                                    bool isNum = isNumber(values[i]);
                                    data += (i == 0 ? "" : ",") + (isNum ? "" : "'") + values[i] + (isNum ? "" : "'");
                                }
                                data += ")";

                                dbManager.write(TABLE, fields, data);
                            }
                        }
                        else error("Not enough arguments, if inside table minimum is 2");
                    } catch (Exception e)
                    {
                        error(e.Message);
                    }
                    goto START;

                // Print the help information
                case "help":
                    printHelp();
                    goto START;

                // Exit the program
                case "exit":
                case "quit":
                    goto EXIT;

                // Print specified data
                case "print":
                    if (command.Count == 3 && command[1].Equals("columns"))
                    {
                        dbManager.printColumns(command[2]);
                        goto START;
                    }
                    else if (command.Count == 2 && command[1].Equals("tables"))
                    {
                        dbManager.printTables();
                        goto START;
                    }
                    else if (command.Count == 2 && command[1].Equals("*"))
                    {
                        for (int j = 0; j < dbManager.tableNames.Count; j++)
                        {
                            dbManager.printSelection(dbManager.readList(dbManager.tableNames[j], "*"));
                        }
                        goto START;
                    }
                    else if (TABLE != null && command[1].Equals("*")) dbManager.printSelection(dbManager.readList(TABLE, command[1], command[2]));
                    else if (command.Count <= 2) error("Not enough arguments, you must specify database table and selector!");
                    else dbManager.printSelection(dbManager.readList(command[2], command[1]));
                    goto START;

                // Delete a database entry
                case "delete":
                    if (command.Count < 2) error("Not enough arguments, you must specify database table and delete condition!");
                    else if (TABLE != null) dbManager.delete(TABLE, command[1]);
                    else dbManager.delete(command[1], command[2]);
                    Console.WriteLine("Database entry deleted!");
                    goto START;

                // Remove a database table
                case "remove":
                    if (command.Count < 2)
                    {
                        error("Not enough arguments, you must specify database table to be dropped!");
                        goto START;
                    }
                    try
                    {
                        dbManager.dropTable(command[1]);
                        Console.WriteLine($"Database table {command[1]} removed!");
                    } catch (Exception e)
                    {
                        error(e.Message);
                    }
                    goto START;

                // Execute raw SQL command
                case "raw":
                    dbManager.exec(command[1]);
                    Console.WriteLine($"Command <{command[1]}> has been executed!");
                    goto START;

                // Command doesn't exist
                default:
                    error("Command doesn't exist\n");
                    goto START;
            }
        EXIT:
            dbManager.close();
            return;
        }
    }
}