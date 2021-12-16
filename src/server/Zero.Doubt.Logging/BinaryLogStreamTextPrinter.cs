using System.Text;

namespace Zero.Doubt.Logging
{
    public static class BinaryLogStreamTextPrinter
    {
        public static string PrintTree(BinaryLogStreamReader.Node rootNode)
        {
            StringBuilder output = new();

            foreach (var topLevelNode in rootNode.Nodes)
            {
                PrintNode(topLevelNode);
            }

            return output.ToString();

            void PrintNode(BinaryLogStreamReader.Node node)
            {
                var timeText = node.Time.ToString("HH:mm:ss.fff");
                var durationText = node.Duration.HasValue
                    ? ((int) node.Duration.Value.TotalMilliseconds).ToString()
                    : string.Empty;
                var levelText = GetLevelText(node);
                var indentText = new string(' ', node.Depth * 3);
                var spanIcon = node.IsSpan ? '>' : '-';
                var levelIcon = GetLevelIcon(node.Level); 

                output.AppendLine(
                    $"{timeText}|{durationText,-10}|{levelText}|{levelIcon,3}{indentText}{spanIcon,-2}{node.MessageId}");

                foreach (var subNode in node.Nodes)
                {
                    PrintNode(subNode);
                }
            }

            string GetLevelText(BinaryLogStreamReader.Node node)
            {
                switch (node.Level)
                {
                    case LogLevel.Audit:
                        return "AUD"; 
                    case LogLevel.Critical:
                        return "CRI"; 
                    case LogLevel.Error:
                        return "ERR"; 
                    case LogLevel.Warning:
                        return "WRN"; 
                    case LogLevel.Success:
                        return "suc"; 
                    case LogLevel.Info:
                        return "inf"; 
                    default:
                        return "dbg";
                }
            }

            string GetLevelIcon(LogLevel level)
            {
                switch (level)
                {
                    case LogLevel.Critical:
                        return "[X]";
                    case LogLevel.Error:
                        return "[x]";
                    case LogLevel.Warning:
                        return "[!]";
                    default:
                        return string.Empty;
                }
            }
        }
    }
}