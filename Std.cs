using System;
using System.Text.RegularExpressions;
namespace CodeStudio
{
	public static class ArgumentsCheck {
		public static void Check(int expected, int got, Function function) {
			if (got != expected) {
				throw new Exception(String.Format("Неверное кол-во аргументов в функции {0}", function));
			}
		}
		
		public static void CheckRange(int from, int to, int got, Function function) {
			if (from > got || got > to)
           	 	throw new ArgumentOutOfRangeException(String.Format("В функции {0} ожидалось от {1} до {2} аргументов", function, from, to));
		}
		
		public static void CheckOrOr(int expectedOne, int expectedTwo, int got, Function function) {
			if (expectedOne != got && expectedTwo != got)
            	throw new ArgumentException(String.Format("In function {0} : {1} or {2} arguments expected, got {3}", function, expectedOne, expectedTwo, got));
		}
		
		public static void checkAtLeast(int expected, int got) {
        	if (got < expected) 
        		throw new ArgumentException(String.Format("At least {0} {1} expected, got {2}", expected, pluralize(expected), got));
    	}
		
		private static String pluralize(int count) {
        	return (count == 1) ? "argument" : "arguments";
    	}
	}
	
	public static class ValueUtils {
		public static Function consumeFunction(Value value, int argumentNumber) {
        	return consumeFunction(value, " at argument " + (argumentNumber + 1));
    	}

    	public static Function consumeFunction(Value value, String errorMessage) {
        	Types type = value.type();
        	if (type != Types.FUNCTION) {
        		throw new Exception("Function expected" + errorMessage + ", but found " + type);
        	}
        	return ((FunctionValue) value).getValue();
    	}
	}
	
	// Работа с регулярными выражениями (Поиск первого вхождения)
	public class std_RegexMatch : Function {
		public Value execute(Value[] args) {
			ArgumentsCheck.CheckOrOr(2,3, args.Length, this);
			string sourceString = args[0].asString();
			string regex = args[1].asString();
			
			try{
             	Match match = (new Regex(regex, RegexOptions.IgnoreCase)).Match(sourceString);
             	ArrayValue matcharray = new ArrayValue(1);
             	
             	for (int i = 0; i < 1; i++)
             	{
             		matcharray.setValue(i, new StringValue(match.Value));
             	}
             	if (!match.Success)
             	{
             		matcharray.setValue(0, new StringValue("нет совпадений!"));
             	}
             	return new ArrayValue(matcharray);
             }
            catch (Exception)
            {
            	throw new Exception("Неправильное регулярное выражение");
            }
		}
	}
	
	// Работа с регулярными выражениями (Поиск всех вхождений)
	public class std_RegexMatches : Function {
		public Value execute(Value[] args) {
			ArgumentsCheck.CheckOrOr(2,3, args.Length, this);
			string sourceString = args[0].asString();
			string regex = args[1].asString();
			
			try{
             	MatchCollection matchcollection = (new Regex(regex, RegexOptions.IgnoreCase)).Matches(sourceString);
				ArrayValue matcharray = new ArrayValue(matchcollection.Count);
             	
             	for (int i = 0; i < matchcollection.Count; i++)
             	{
             		matcharray.setValue(i, new StringValue(matchcollection[i].Value));
             	}
             	if (matchcollection.Count < 1)
             	{
             		matcharray.setValue(0, new StringValue("нет совпадений!"));
             	}
             	return new ArrayValue(matcharray);
             }
            catch (Exception)
            {
            	throw new Exception("Неправильное регулярное выражение");
            }
		}
	}

	public class std_ReplaceAll : Function {
		public Value execute(Value[] args) {
			ArgumentsCheck.CheckOrOr(2,3, args.Length, this);
			string sourceString = args[0].asString();
			string pattern = args[1].asString();
			string replacement = args[2].asString();
			return new StringValue(Regex.Replace(sourceString, pattern, replacement, RegexOptions.IgnoreCase));
		}
	}
}
