using System.Text.RegularExpressions;
var p = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*+\(";
try { var r = new Regex(p, RegexOptions.Compiled); Console.WriteLine("ok"); }
catch (Exception e) { Console.WriteLine(e); }
