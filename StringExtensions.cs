public static class StringExtensions
{
  public static List<string> GetLines(this string data)
  {
    List<string> result = new List<string>();
    using var reader = new StringReader(data ?? "");
    string line;
    while (null != (line = reader.ReadLine()))
    {
      result.Add(line);
    }
    return result;
  }
}