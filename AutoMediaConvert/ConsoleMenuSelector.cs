namespace AutoMediaConvert
{
    public class ConsoleMenuSelector
    {
        public static int Select(string[] options, string title = "请选择：")
        {
            int index = 0;
            ConsoleKey key;

            Console.Clear();
            Console.WriteLine(title);
            Console.WriteLine();

            do
            {
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("→ " + options[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("  " + options[i]);
                    }
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow)
                    index = (index == 0) ? options.Length - 1 : index - 1;
                else if (key == ConsoleKey.DownArrow)
                    index = (index + 1) % options.Length;

                Console.SetCursorPosition(0, Console.CursorTop - options.Length);
            } while (key != ConsoleKey.Enter);

            Console.Clear();
            return index;
        }

        public static string Choose(string[] options, string title = "请选择：")
        {
            int selectedIndex = Select(options, title);
            return options[selectedIndex];
        }
    }

}
