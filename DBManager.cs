using Microsoft.Data.Sqlite;

namespace DBInter
{
    public class DBManager
    {
        private SqliteConnection? connection;
        private SqliteCommand? command;
        private int columns = 0;
        public List<string> columnNames = new List<string>();
        public List<string> tableNames = new List<string>();

        public DBManager() { }

        public DBManager(string file)
        {
            createConnection(file);
            openConnection();
            fillTableNames();
        }

        public SqliteConnection createConnection(string file)
        {
            connection = new SqliteConnection($"Data source={file}");
            command = connection.CreateCommand();
            return connection;
        }

        public SqliteConnection? closeConnection()
        {
            if (connection == null) return null;
            connection.Close();
            return connection;
        }

        public SqliteConnection? close() { return closeConnection(); }

        public SqliteConnection? getConnection() { return connection; }

        public SqliteConnection openConnection()
        {
            if (connection == null) throw new Exception("Database connection doesn't exist.");
            connection.Open();
            return connection;
        }

        public string setCommand(string data)
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            command.CommandText = data;
            return command.CommandText;
        }

        public string addCommand(string data)
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            command.CommandText += data;
            return command.CommandText;
        }

        public void addCommandParametersWithValue(string[,] parameters)
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            for (int i=0; i < parameters.GetLength(0); i++)
            {
                command.Parameters.AddWithValue(parameters[i, 0], parameters[i, 1]);
            }
        }

        public SqliteCommand? getCommand() { return command; }

        public void clearCommandParameters()
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            command.Parameters.Clear();
        }

        public void exec()
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            command.ExecuteNonQuery();
        }

        public void exec(string data)
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            setCommand(data);
            command.ExecuteNonQuery();
        }

        public void createTable(string table, string fields)
        {
            for (int i=0; i<fields.Length; i++)
            {
                if (fields[i] == ',') columns++;
                if (fields[i] == ')') break;
            }
            columns++;

            exec($"CREATE TABLE IF NOT EXISTS {table} {fields};");
            if (!tableNames.Contains(table)) tableNames.Add(table);
        }

        public void dropTable(string table)
        {
            exec($"DROP TABLE {table};");
        }

        public void write(string table, string fields, string values)
        {
            if (command == null) throw new Exception("Database command wasn't established.");

            // Getting the table column names
            setCommand($"SELECT * FROM {table}");
            SqliteDataReader reader = command.ExecuteReader();
            columnNames.Clear();
            columns = reader.FieldCount;

            for (int i = 0; i < columns; i++)
                columnNames.Add(reader.GetName(i));
            
            reader.Close();
            
            // Writing into the database
            exec($"INSERT OR REPLACE INTO {table} {fields} VALUES {values};");
        }

        public List<string[]> readList(string table, string selector)
        {
            if (command == null) throw new Exception("Database command wasn't established.");
            List<string[]> data = new List<string[]>();

            setCommand($"SELECT {selector} FROM {table};");
            SqliteDataReader reader = command.ExecuteReader();
            columns = reader.FieldCount;
            columnNames.Clear();
            for (int i = 0; i < columns; i++)
                columnNames.Add(reader.GetName(i));

            while (reader.Read())
            {
                string[] values = new string[columns];

                for (int i = 0; i < columns; i++)
                    values[i] = reader.IsDBNull(i) ? "" : reader.GetString(i);

                data.Add(values);
            }
            reader.Close();
            return data;
        }

        public List<string[]> readList(string table, string selector, string condition)
        {
            if (command == null) throw new Exception("Database command wasn't established.");

            setCommand($"SELECT {selector} FROM {table} WHERE {condition};");

            List<string[]> data = new List<string[]>();
            SqliteDataReader reader = command.ExecuteReader();
            columns = reader.FieldCount;
            columnNames.Clear();
            for (int i = 0; i < columns; i++)
                columnNames.Add(reader.GetName(i));

            while (reader.Read())
            {
                string[] values = new string[columns];

                for (int i = 0; i < columns; i++)
                    values[i] = reader.GetString(i);

                data.Add(values);
            }
            reader.Close();
            return data;
        }

        public void delete(string table, string condition)
        {
            if (command == null) throw new Exception("Database command wasn't established");
            exec($"DELETE FROM {table} WHERE {condition};");
        }

        public void printSelection(List<string[]> selection)
        {
            int[] widths = new int[columns];
            for (int i=0; i<selection.Count; i++)
            {
                for (int j=0; j<columns; j++)
                {
                    int len = selection[i][j].Length;
                    if (len > widths[j]) widths[j] = len;
                }
            }

            // Check if maximum width in a column is greater than the column name
            for (int i=0; i<columns; i++)
                if (widths[i] < columnNames[i].Length) widths[i] = columnNames[i].Length;

            int sum = columns * 3 - 1;
            for (int i = 0; i < columns; i++)
                sum += widths[i];

            // For the "+" in the table printout
            int[] widthsP = new int[columns - 1];
            for (int i = 0; i < columns - 1; i++)
            {
                int intermediateSum = 0;
                for (int j = 0; j <= i; j++)
                {
                    intermediateSum += widths[j];
                }
                widthsP[i] = intermediateSum + i * 3;
            }

            // Write the column names
            Console.Write("\n|");
            for (int i=0; i<columns; i++)
                Console.Write(" " + columnNames[i] + padding(columnNames[i], widths[i]) + " |");
            Console.Write("\n");

            Console.Write("|");
            for (int i = 0, indexer = 0; i < sum; i++)
            {
                if (columns > 1 && i == widthsP[indexer] + 2)
                {
                    Console.Write('+');
                    indexer += indexer < widthsP.Length - 1 ? 1 : 0;
                }
                else Console.Write('-');
            }
            Console.Write("|");

            Console.Write('\n');
            for (int i=0; i<selection.Count; i++)
            {
                Console.Write('|');
                for (int j = 0; j < columns; j++)
                    Console.Write(" " + selection[i][j] + padding(selection[i][j], widths[j]) + " |");
                Console.Write("\n");
            }

            Console.Write("|");
            for (int i = 0, indexer = 0; i < sum; i++)
            {
                if (columns > 1 && i == widthsP[indexer] + 2)
                {
                    Console.Write('+');
                    indexer += indexer < widthsP.Length - 1 ? 1 : 0;
                }
                else Console.Write('-');
            }
            Console.Write("|\n\n");
        }

        public void printColumns(string table)
        {
            if (command == null) throw new Exception("Database command wasn't established");
            setCommand($"SELECT * FROM {table}");
            SqliteDataReader reader = command.ExecuteReader();
            columns = reader.FieldCount;

            for (int i=0; i<columns; i++)
                columnNames.Add(reader.GetName(i));

            Console.Write("\n|");
            for (int i = 0; i < columns; i++)
                Console.Write(" " + columnNames[i] + " |");
            Console.Write("\n\n");
            reader.Close();
        }

        public void printTables()
        {
            if (command == null) throw new Exception("Database command wasn't established");
            setCommand("SELECT name FROM sqlite_master WHERE type='table';");
            SqliteDataReader reader = command.ExecuteReader();

            Console.Write("\n|");
            while(reader.Read())
                Console.Write(" " + reader.GetString(0) + " |");
            Console.Write("\n\n");
            reader.Close();
        }

        public void fillTableNames()
        {
            if (command == null) throw new Exception("Database command wasn't established");
            setCommand("SELECT name FROM sqlite_master WHERE type='table';");
            SqliteDataReader reader = command.ExecuteReader();
            
            while(reader.Read())
                tableNames.Add(reader.GetString(0));
            reader.Close();
        }

        public void clear(string table)
        {
            exec($"DELETE FROM {table};");
        }

        public string padding(string data, int length)
        {
            string padding = "";

            for (int i = 0; i < length - data.Length; i++)
                padding += " ";
            
            return padding;
        }
    }
}