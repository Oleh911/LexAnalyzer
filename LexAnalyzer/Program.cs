using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public enum TokenType
{
    NUM = 0,
    OP = 1,
    LPAREN = 2,
    RPAREN = 3,
    END = 4
}

public enum ValidationState
{
    EXPECT_OPERAND = 0,
    EXPECT_OPERATOR = 1,
    ACCEPT = 2
}

public class ExpressionValidator
{

    private static readonly int[,] TransitionTable = new int[,]
    {
        { (int)ValidationState.EXPECT_OPERATOR, -1, (int)ValidationState.EXPECT_OPERAND, -1, -1 },
        { -1, (int)ValidationState.EXPECT_OPERAND, -1, (int)ValidationState.EXPECT_OPERATOR, (int)ValidationState.ACCEPT },
        { -1, -1, -1, -1, -1 }
    };

    private static readonly Regex NumberRegex = new Regex(@"^[+-]?(\d+(\.\d*)?|\.\d+)$", RegexOptions.Compiled);

    public static List<(TokenType Type, string Value)> Tokenize(string expr)
    {
        var tokens = new List<(TokenType Type, string Value)>();
        int i = 0;
        TokenType? previousTokenType = null;

        while (i < expr.Length)
        {
            char c = expr[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            bool isSign = (c == '+' || c == '-');
            bool isContextForUnary = previousTokenType == null || previousTokenType == TokenType.OP || previousTokenType == TokenType.LPAREN;

            if (char.IsDigit(c) || c == '.' || (isSign && isContextForUnary))
            {
                string numStr = "";
                numStr += c;
                i++;

                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                {
                    numStr += expr[i];
                    i++;
                }

                if (!NumberRegex.IsMatch(numStr))
                {
                    throw new ArgumentException($"Лексична помилка: некоректний формат числа '{numStr}'");
                }

                tokens.Add((TokenType.NUM, numStr));
                previousTokenType = TokenType.NUM;

                continue;
            }

            if (c == '(')
            {
                tokens.Add((TokenType.LPAREN, "("));
                previousTokenType = TokenType.LPAREN;
                i++;
            }
            else if (c == ')')
            {
                tokens.Add((TokenType.RPAREN, ")"));
                previousTokenType = TokenType.RPAREN;
                i++;
            }

            else if ("+-*/".IndexOf(c) != -1)
            {
                tokens.Add((TokenType.OP, c.ToString()));
                previousTokenType = TokenType.OP;
                i++;
            }
            else
            {
                throw new ArgumentException($"Лексична помилка: недопустимий символ '{c}'");
            }
        }

        tokens.Add((TokenType.END, "END"));
        return tokens;
    }

    public static bool ValidateExpression(string expr)
    {
        List<(TokenType Type, string Value)> tokens;

        try
        {
            tokens = Tokenize(expr);
            Console.WriteLine("  [INFO] Токени: " + string.Join(", ", tokens));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"  [FAIL] {ex.Message}");
            return false;
        }

        int currentState = (int)ValidationState.EXPECT_OPERAND;
        int parenBalance = 0;

        foreach (var token in tokens)
        {
            int tokenIndex = (int)token.Type;

            int nextState = TransitionTable[currentState, tokenIndex];

            if (nextState == -1)
            {
                Console.WriteLine($"  [FAIL] Синтаксична помилка: неочікуваний токен '{token.Value}' ({token.Type}) після стану {((ValidationState)currentState)}.");
                return false;
            }

            if (token.Type == TokenType.LPAREN)
            {
                parenBalance++;
            }
            else if (token.Type == TokenType.RPAREN)
            {
                parenBalance--;
                if (parenBalance < 0)
                {
                    Console.WriteLine("  [FAIL] Синтаксична помилка: зайва закриваюча дужка.");
                    return false;
                }
            }

            currentState = nextState;
        }

        bool isAcceptState = currentState == (int)ValidationState.ACCEPT;
        bool isBalanced = parenBalance == 0;

        if (!isBalanced)
        {
            Console.WriteLine($"  [FAIL] Синтаксична помилка: незакриті дужки (баланс: {parenBalance}).");
            return false;
        }

        if (isAcceptState && isBalanced)
        {
            return true;
        }

        Console.WriteLine("  [FAIL] Вираз завершився некоректно.");
        return false;
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Лексичний аналізатор математичних виразів");
        Console.WriteLine("Введіть вираз (або 'exit' для виходу):");

        while (true)
        {
            Console.Write("\n> ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().ToLower() == "exit") break;

            bool isValid = ExpressionValidator.ValidateExpression(input);

            if (isValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] Вираз коректний.");
            }

            Console.ResetColor();
        }
    }
}