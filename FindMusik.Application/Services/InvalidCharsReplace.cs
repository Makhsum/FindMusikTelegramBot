namespace FindMusik.Application.Services;

public class InvalidCharsReplace
{
   private char[] _invalidFileNameChars = { '\\','/',':','*','?','"','<','>','|',};
   
   public string ReplaceAsync(string name)
   {
      string text = name;
      foreach (var nameChar in _invalidFileNameChars)
      {
         text = text.Replace(nameChar, '_');
      }
      return text;
   }
   
}