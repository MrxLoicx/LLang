using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
namespace CodeStudio
{
	public interface Expression {
		Value eval();
	}
	
	public class ValueExpression : Expression {
		private Value value;
		
		public ValueExpression(double value) {
			this.value = new NumberValue(value);
		}
		
		public ValueExpression(string value) {
			this.value = new StringValue(value);
		}

		public Value eval() {
			return value;
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return value.asString();
		} 
	}
	
	public class BinaryExpression : Expression {
		private Expression expr1, expr2;
		private Operator operation;
		
		public BinaryExpression(Operator operation ,Expression expr1, Expression expr2) {
			this.operation = operation;
			this.expr1 = expr1;
			this.expr2 = expr2;
		}
		
		public Value eval() {
			Value value1;
			
			if (operation == Operator.ELVIS) {
        		try {
            		value1 = expr1.eval();
        		} catch (Exception) {
            		value1 = null;
        		}
        		if (value1 == null) {
            		return expr2.eval();
        		}
        		return value1;
			}
			
			value1 = expr1.eval();
			Value value2 = expr2.eval();
			
			if ((value1 is StringValue) || (value1 is ArrayValue)) {
				string string1 = value1.asString();
				
				switch (operation) {
					case Operator.MULTIPLY: {
						int iterations = value2.asInt();
						StringBuilder buffer = new StringBuilder();
							
						for (int i = 0; i < iterations; i++) {
							buffer.Append(string1);
						}
						return new StringValue(buffer.ToString());
					}
					case Operator.ADD: return new StringValue(string1 + value2.asString());
				}
			}
			
			switch (operation) {
					case Operator.ADD: return new NumberValue(expr1AsDouble() + expr2AsDouble());
					case Operator.SUBTRACT: return new NumberValue(expr1AsDouble() - expr2AsDouble());
					case Operator.MULTIPLY: return new NumberValue(expr1AsDouble() * expr2AsDouble());
					case Operator.DIVIDE: return new NumberValue(expr1AsDouble() / expr2AsDouble());
					case Operator.REMAINDER: return new NumberValue(expr1AsDouble() % expr2AsDouble());
					case Operator.POWER: return new NumberValue(Math.Pow(expr1AsDouble(), expr2AsDouble()));
					case Operator.BITAND: return new NumberValue(expr1AsInt() & expr2AsInt());
					case Operator.BITOR: return new NumberValue(expr1AsInt() | expr2AsInt());
					case Operator.BITXOR: return new NumberValue(expr1AsInt() ^ expr2AsInt());
					case Operator.LSHIFT: return new NumberValue(expr1AsInt() << expr2AsInt());
					case Operator.RSHIFT: return new NumberValue(expr1AsInt() >> expr2AsInt());
					default: throw new Exception("Error with operation");
			}
		}

		private int expr1AsInt() {
			return expr1.eval().asInt();
		}
		
		private int expr2AsInt() {
			return expr2.eval().asInt();
		}
		
		private double expr1AsDouble() {
			return expr1.eval().asDouble();
		}
		
		private double expr2AsDouble() {
			return expr2.eval().asDouble();
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return String.Format("({0} {1} {2})", expr1, operation, expr2);
		} 
	}
	
	public class UnaryExpression : Expression {
		private Expression expr1;
		private Operator operation;
		
		public UnaryExpression(Operator operation, Expression expr1) {
			this.operation = operation;
			this.expr1 = expr1;
		}
		
		public Value eval() {
			Value value = expr1.eval();
			switch (operation) {
					case Operator.ECXL: return new NumberValue(value.asDouble() == 0);
					case Operator.BITNOT: return new NumberValue(~ value.asInt());
					case Operator.SUBTRACT: return new NumberValue(-value.asDouble());
					case Operator.INCREMENT_POSTFIX: {
						 if (expr1 is Accessible) {
                    		((Accessible) expr1).set(increment(value));
                    		return value;
                		}
                		return increment(value);
					}
					case Operator.DECREMENT_POSTFIX: return new NumberValue(expr1.eval().asDouble() - 1);
				case Operator.ADD: return new NumberValue(expr1.eval().asDouble());
				default:
					return new NumberValue(expr1.eval().asDouble());
			}
		}
		
		private Value increment(Value value) {
			if (value.type() == Types.NUMBER) {
				return new NumberValue(value.asInt() + 1);
			}
			throw new Exception("Invalid increment syntax");
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString() {
			return String.Format("{0} {1}", operation, expr1);
		}
	}
	
	public static class Variables {
		public static Dictionary<string, Value> variables;
		private static NumberValue ZERO = new NumberValue(0);
		private static Stack<Dictionary<string, Value>> stack;
		
		static Variables() {
			stack = new Stack<Dictionary<string, Value>>();
			variables = new Dictionary<string, Value>();
			variables.Add("PI", new NumberValue(Math.PI));
			variables.Add("true", new NumberValue(true));
			variables.Add("false", new NumberValue(false));
			variables.Add("TAU", new NumberValue(Math.PI * 2));
			variables.Add("RAD2DEG", new NumberValue(180 / Math.PI));
			variables.Add("DEG2RAD", new NumberValue(Math.PI / 180));
			variables.Add("E", new NumberValue(Math.E));
			variables.Add("GOLDEN_RATIO", new NumberValue(1.6180339887498948482));
			variables.Add("SILVER_RATIO", new NumberValue(2.4142135623));
			variables.Add("BRONSE_RATIO", new NumberValue(3.30277563773));
			variables.Add("MAX_DOUBLE", new NumberValue(Double.MaxValue));
			variables.Add("MAX_SINGLE", new NumberValue(Double.MaxValue));
			Push(); // добавляем в стек системные константы
		}
		// Положить переменные в стек
		public static void Push() {
			// копируем значения переменных в стек
			stack.Push(new Dictionary<string, Value>(variables));
		}
		
		// Извлечь переменные из стека
		public static void Pop() {
			variables = stack.Pop();
		}
		
		public static bool isExists(string key) {
			return variables.ContainsKey(key);
		}
		
		public static Value get(string key) {
			if (!isExists(key)) return ZERO;
			Value value;
			variables.TryGetValue(key, out value);
			return value;
		}
		
		public static void set(string key, Value value) {
			variables.Add(key, value);
		}
	
		public static void assign(string key, Value value) {
			if (variables.ContainsKey(key)) {
				variables[key] = value;
			}
		}
		
		public static void remove(string key) {
			variables.Remove(key);
		}
		// Удаляет все переменные(нужно при перезапуске программы)
		public static void Clear() {
			variables.Clear();
			Pop(); // Добавляем в variables системные константы 
			Push(); // Копируем в стек системные константы
		}
		
		public static string[] GetAllNameVariables() {
			var collection = variables.Keys.ToArray();
			return collection;
		}
	}
	
	public class VariableExpression : Expression {
		private string name;
		
		public VariableExpression(string name) {
			this.name = name;
		}
		
		public Value eval() {
			if (!Variables.isExists(name)) {
				return null;
			}
			return Variables.get(name);
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return name;
		} 
	}
	
	public interface Statement {
		void execute();
	}
	// оператор присвоения
	public class AssignmentStatement : Statement {
		private string variable;
		private Expression expression;
		
		public AssignmentStatement(string variable, Expression expression) {
			this.variable = variable;
			this.expression = expression;
		}
		
		private Value expressionOrNull() {
			if (expression == null) return new NumberValue(0);
			else return expression.eval();
		}
		
		public void execute() {
			Value result = expressionOrNull();
			
			if (!Variables.isExists(variable)) {
				Variables.set(variable, result);
			} else {
				Variables.assign(variable, result);
			}
		}
		
		public override string ToString()
		{
			return String.Format("{0} = {1}", variable, expression);
		} 
	}
	// Оператор print
	public class PrintStatement : Statement {
		private Expression expression;
		
		public PrintStatement(Expression expression) {
			this.expression = expression;
		}
		
		public void execute() {
			Program.IDE.richErrorConsole.Text += expression.eval().asString();
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "print " + expression.eval();
		} 
	}
	
	// Оператор print
	public class PrintlnStatement : Statement {
		private Expression expression;
		
		public PrintlnStatement(Expression expression) {
			this.expression = expression;
		}
		
		public void execute() {
			Program.IDE.richErrorConsole.Text += expression.eval().asString() + Environment.NewLine;
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "println " + expression.eval();
		} 
	}
	// Интерфейс для типов данных
	public interface Value {
		// Object raw();
		int asInt();
		double asDouble();
		string asString();
		long asLong();
		byte asByte();
		char asChar();
		bool asBool();
		ushort asUShort();
		Types type();
	}

	public enum Types {
		OBJECT = 0,
        NUMBER = 1,
        STRING = 2,
        BOOL = 3,
        ARRAY = 4,
        MAP = 5,
        FUNCTION = 6,
        CLASS = 7
	}
	
	public class NumberValue : Value{
		public static Value ZERO = new NumberValue(0);
	   	private double value;
		
	   	public NumberValue(bool value) {
	   		this.value = value ? 1 : 0;
		}
	   	
		public NumberValue(double value) {
			this.value = value;
		}

	   	public Types type() {
        	return Types.NUMBER;
    	}

	   	public int asInt() {
	   		return (int) value;
		}
	   	
	   	public byte asByte() {
	   		return Convert.ToByte(value);
		}
	   	
	   	public ushort asUShort() {
	   		return (ushort) value;
	   	}
	   	
	   	public long asLong() {
	   		return (long) value;
		}
	   	
	   	public bool asBool() {
	   		if (value != 0) {
	   			return true;
	   		} else {
	   			return false;
	   		}
	   	}
	   	
	   	public char asChar() {
	   		try {
	   			return (char) value;
	   		} catch(Exception) {
	   			throw new InvalidCastException("Невозможно преобразовать в тип char");
	   		}
		}
	   	
		public double asDouble() {
			return value;
		}
		public string asString() {
			return value.ToString();
		} 
	   	
	   	public override string ToString()
		{
			return asString();
		} 
	}
	
	public class StringValue : Value{
		public static StringValue EMPTY = new StringValue("");
		private string value;
		
		public StringValue(string value) {
			this.value = value;
		}
		
		public StringValue(Value value) {
			this.value = value.asString();
		}
		
		public StringValue(char value) {
			this.value = Convert.ToString(value);
		}
		
		public StringValue(bool value) {
			this.value = Convert.ToString(value);
		}
		
		public double asDouble() {
			try {
				return Double.Parse(value);
			} catch (FormatException) {
				return 0;
			}
		}
		
		public ushort asUShort() {
		    try {
				return ushort.Parse(value);
			} catch (FormatException) {
				return 0;
			}
		}
		
		public byte asByte() {
			try {
				return Byte.Parse(value);
			} catch (FormatException) {
				throw new InvalidCastException("Невозможно преобразовать string в byte");
			}
		}
		
		public long asLong() {
			try {
				return Convert.ToInt64(value);
			} catch(Exception) {
				throw new InvalidCastException("Невозможно преобразовать string в тип long");
			}
		}
	   	
	   	public bool asBool() {
			if (!String.IsNullOrEmpty(value)) {
	   			return true;
	   		} else {
	   			return false;
	   		}
	   	}
	   	
	   	public char asChar() {
	   		try {
				return Convert.ToChar(value);
	   		} catch(Exception) {
	   			throw new InvalidCastException("Невозможно преобразовать string в тип char");
	   		}
		}
		
		public int length() {
        	return value.Length;
    	}
    
    	public Types type() {
        	return Types.STRING;
    	}

    	public Object raw() {
        	return value;
    	}
		
		public int asInt() {
			try {
				return Convert.ToInt32(value);
			} catch(Exception) {
				throw new Exception("Невозможно преобразовать строку в число типа int");
			}
		}
		
		public string asString() {
			return value;
		}
		
		public override string ToString()
		{
			return asString();
		} 
	}
	
	public enum Operator {
		[Description("+")]
		ADD,
		[Description("-")]
		SUBTRACT,
		[Description("*")]
		MULTIPLY,
		[Description("/")]
		DIVIDE,
		[Description("%")]
		REMAINDER,
		[Description("**")]
		POWER,
		[Description("==")]
		EQUALS,
		[Description("!=")]
		NOT_EQUALS,
		[Description("<")]
		LT,
		[Description("<=")]
		LTEQ,
		[Description(">")]
		GT,
		[Description(">=")]
		GTEQ,
		[Description("&&")]
		AND,
		[Description("||")]
		OR,
		[Description("^^")]
		XOR,
		[Description("&")]
		BITAND,
		[Description("|")]
		BITOR,
		[Description("~")]
		BITNOT,
		[Description("^")]
		BITXOR,
		[Description("<<")]
        LSHIFT,
		[Description(">>")]
        RSHIFT,
		[Description("??")]
		NULL_COALESCE,
		[Description("?:")]
		ELVIS,
		[Description("++")]
		INCREMENT_POSTFIX,
		[Description("--")]
		DECREMENT_POSTFIX,
		[Description("!")]
		ECXL
	}
		
	public class ConditionalExpression : Expression {
		private Expression expr1, expr2;
		private Operator operation;
		
		public ConditionalExpression(Operator operation ,Expression expr1, Expression expr2) {
			this.operation = operation;
			this.expr1 = expr1;
			this.expr2 = expr2;
		}

		public Value eval() {
			switch (operation) {
            	case Operator.AND:
                	return new NumberValue((expr1AsInt() != 0) && (expr2AsInt() != 0));
            	case Operator.OR:
                	return new NumberValue((expr1AsInt() != 0) || (expr2AsInt() != 0));
            	case Operator.NULL_COALESCE:
                	return nullCoalesce();

           		 default:
                	return new NumberValue(evalAndCompare());
			}
		}
		
		private bool evalAndCompare() {
	        Value value1 = expr1.eval();
	        Value value2 = expr2.eval();
	
	        double number1, number2;
	        if (value1.type() == Types.NUMBER) {
	            number1 = value1.asDouble();
	            number2 = value2.asDouble();
	        } else {
	        	number1 = value1.asString().CompareTo(value2);
	            number2 = 0;
	        }
	
	        switch (operation) {
	            case Operator.EQUALS: return number1 == number2;
	            case Operator.NOT_EQUALS: return number1 != number2;
	            case Operator.LT: return number1 < number2;
	            case Operator.LTEQ: return number1 <= number2;
	            case Operator.GT: return number1 > number2;
	            case Operator.GTEQ: return number1 >= number2;
	            default: throw new Exception(operation.ToString());
	        }
    	}
		
		
		 private Value nullCoalesce() {
			Value value1;
        	try {
            	value1 = expr1.eval();
        	} catch (Exception) {
            	value1 = null;
        	}
        	if (value1 == null) {
            	return expr2.eval();
        	}
        	return value1;
    	}
		
		private int expr1AsInt() {
        	return expr1.eval().asInt();
    	}

    	private int expr2AsInt() {
        	return expr2.eval().asInt();
    	}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return String.Format("({0} {1} {2})", expr1, operation, expr2);
		} 
	}
	
	public class TernaryExpression : Expression {
		public Expression condition;
	    public Expression trueExpr, falseExpr;
	
	    public TernaryExpression(Expression condition, Expression trueExpr, Expression falseExpr) {
	        this.condition = condition;
	        this.trueExpr = trueExpr;
	        this.falseExpr = falseExpr;
	    }
	
	    public Value eval() {
	        if (condition.eval().asInt() != 0) {
	            return trueExpr.eval();
	        } else {
	            return falseExpr.eval();
	        }
	    }
	    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	    
	    public override string ToString() {
	    	return String.Format("({0} ? {1} : {2})", condition, trueExpr, falseExpr);
	    }
	}
	
	// Условный оператор
	public class IfStatement : Statement {
		private Expression expression;
		private Statement ifStatement, elseStatement;
		
		public IfStatement(Expression expression, Statement ifStatement, Statement elseStatement) {
			this.expression = expression;
			this.ifStatement = ifStatement;
			this.elseStatement = elseStatement;
		}
		
		public void execute() {
			double result = expression.eval().asDouble();
			if (result != 0) {
				ifStatement.execute();
			} else if (elseStatement != null) {
				elseStatement.execute();
			}
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("if ").Append(expression).Append(' ').Append(ifStatement);
			if (elseStatement != null) {
				result.Append("\nelse ").Append(elseStatement);
			}
			return result.ToString();
		}
	}
	
	public class ThrowStatement : Statement {
		private Expression expression;
		
		public ThrowStatement(Expression expression) {
			this.expression = expression;
		}
		
		public void execute() {
			throw new Exception(expression.eval().ToString());
		}
	}
	
	public class TryCatchFinallyStatement : Statement {
		private Statement tryStatement;
		private Statement catchStatement;
		private Statement finallyStatement;
		
		public TryCatchFinallyStatement(Statement tryStatement, Statement catchStatement, Statement finallyStatement) {
			this.tryStatement = tryStatement;
			this.catchStatement = catchStatement;
			this.finallyStatement = finallyStatement;
		}
		
		public void execute() {
			if (finallyStatement == null) {
				try {
					tryStatement.execute();
				} catch(Exception) {
					catchStatement.execute();
				}
			} else {
				try {
					tryStatement.execute();
				} catch(Exception) {
					catchStatement.execute();
				} finally {
					finallyStatement.execute();
				}
			}
		}
	}
	
	public class BlockStatement : Statement {
		private List<Statement> statements;
		
		public BlockStatement() {
			this.statements = new List<Statement>();
		}
		
		public void add(Statement statement) {
			statements.Add(statement);
		}
		
		public void execute() {
			foreach (Statement statement in statements) {
				statement.execute();
			}
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			
			foreach (Statement statement in statements) {
				result.Append(statement.ToString()).Append(Environment.NewLine);
			}
			
			return result.ToString();
		} 
	}
	
	public class WhileStatement : Statement {
		private Expression condition;
		private Statement statement;
		
		public WhileStatement(Expression condition, Statement statement) {
			this.condition = condition;
			this.statement = statement;
		}
		
		public void execute() {
			while (condition.eval().asDouble() != 0) {
				try {
					statement.execute();
				} catch (BreakException) {
					break;
				} catch (ContinueException) { }
			}
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "while " + condition + " " + statement;
		} 
	}
	
	public class LoopStatement : Statement {
		private Expression count;
		private Statement statement;
		
		public LoopStatement(Expression count, Statement statement) {
			this.count = count;
			this.statement = statement;
		}
		
		public void execute() {
			int i = count.eval().asInt();
			
			while (i != 0) {
				try {
					statement.execute();
					i--;
				} catch (BreakException) {
					break;
				} catch (ContinueException) { }
			}
		}
		
		public override string ToString()
		{
			return "loop " + count + " " + statement;
		}
	}
	
	public class ForStatement : Statement {
		private Statement initialization;
		private Expression termination;
		private Statement increment;
		private Statement statement;
		
		public ForStatement(Statement initialization, Expression termination, Statement increment, Statement statement) {
			this.initialization = initialization;
			this.termination = termination;
			this.increment = increment;
			this.statement = statement;
		}
		
		public void execute() {
			for (initialization.execute(); termination.eval().asDouble() != 0; increment.execute()) {
			    try {
					statement.execute();
				} catch (BreakException) {
					break;
				}
				catch (ContinueException) { }
			}
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "for (" + initialization + ", " + termination + ", " + increment +  ") " + statement;
		} 
	}
	
	public class ForeachArrayStatement : Statement {
		public String variable;
    	public Expression container;
    	public Statement body;

    	public ForeachArrayStatement(String variable, Expression container, Statement body) {
        	this.variable = variable;
        	this.container = container;
        	this.body = body;
    	}
		
		public void execute() {
    		Value previousVariableValue = (Variables.isExists(variable)) ? Variables.get(variable) : null;
    		Value containerValue = container.eval();
    		
    		switch(containerValue.type()) {
    				case Types.STRING: iterateString(containerValue.asString());
    					break;
    				case Types.ARRAY: iterateArray((ArrayValue) containerValue);
    					break;
    				case Types.MAP: iterateMap((MapValue) containerValue);
    					break;	
    				default: throw new Exception("Cannot iterate " + containerValue);
    		}
		}
		
    	private void iterateString(String str) {
    		foreach (char ch in str) {
    			Variables.assign(variable, new StringValue(ch.ToString()));
            	try {
                	body.execute();
            	} catch (BreakException) {
                	break;
            	} catch (ContinueException) { }
        	}
    	}
    	
    	private void iterateArray(ArrayValue containerValue) {
    		foreach (Value value in containerValue) {
            	Variables.assign(variable, value); 
            	try {
                	body.execute();
            	} catch (BreakException) {
                	break;
            	} catch (ContinueException) { }
        	}
    	}
    	
    	private void iterateMap(MapValue containerValue) {
    		foreach (var entry in containerValue.getMap()) {
	            Variables.assign(variable, new ArrayValue(new Value[] {entry.Key, entry.Value }));
	            try {
	                body.execute();
	            } catch (BreakException) {
	                break;
	            } catch (ContinueException) { }
        	}
    	}
    	
    	public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
    	
		public override string ToString()
		{
			return String.Format("foreach {0} : {1} {2}", variable, container, body);
		}
	}
	
	public class DoWhileStatement : Statement {
		private Expression condition;
		private Statement statement;
		
		public DoWhileStatement(Expression condition, Statement statement) {
			this.condition = condition;
			this.statement = statement;
		}
		
		public void execute() {
			do {
				try {
					statement.execute();
				} catch (BreakException) {
					break;
				} catch (ContinueException) { }
			} while (condition.eval().asDouble() != 0);
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "do  " + statement + "" + " while " +condition;
		} 
	}
	
	public class BreakException : Exception {}
	public class ContinueException : Exception {}
	
	public class BreakStatement : Statement {
		public void execute() {
			throw new BreakException();
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "break";
		}
	}
	
	public class ContinueStatement : Statement {
		public void execute() {
			throw new ContinueException();
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "continue";
		}
	}
	
	public class ReturnStatement : Exception, Statement {
		public Expression expression;
		private Value result;
		
		public ReturnStatement(Expression expression) {
			this.expression = expression;
		}
	
		public void execute() {
			result = expression.eval();
			throw this;
		}
		
		public Value getResult() {
			return result;
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "return";
		}
	}
	
	#region "FUNCTIONS"
	public interface Function {
		Value execute(params Value[] args);
	}
	
	public class FunctionDefine : Statement {
		private string name;
		private Arguments argNames;
		private Statement body;
			
		public FunctionDefine(string name, Arguments argNames, Statement body) {
			this.name = name;
			this.argNames = argNames;
			this.body = body;
		}
		
		public void execute() {
			if (!IsExists(name)) {
				Functions.set(name, new UserDefinedFunction(argNames, body));
			} else {
				Functions.get(name);
			}
		}
		
		public void assign() {
			Functions.assign(name, new UserDefinedFunction(argNames, body));
		}
		
		public bool IsExists(string name) {
			return Functions.isExists(name);
		}
		
		public override string ToString()
		{
			return "def (" + argNames + " )" + body;
		} 
	}
	
	public class UserDefinedFunction : Function {
		public Arguments arguments;
	    public Statement body;
	    
	    public UserDefinedFunction(Arguments arguments, Statement body) {
	        this.arguments = arguments;
	        this.body = body;
	    }
		// Кол-во аргументов функции
		public int getArgsCount() {
			return arguments.size();
		}
		// Имя аргумента по указанному индексу
		public string getArgsName(int index) {
			if (index < 0 || index >= getArgsCount()) {
				return "";
			}
			return arguments.get(index).getName();
		}
		
		public Value execute(params Value[] args) {
			try {
				body.execute();
				return NumberValue.ZERO;
			} catch (ReturnStatement rt) {
				return rt.getResult();
			}
		}
	}
	// Функции в выражениях
	public class Functions {
		public static NumberValue ZERO = new NumberValue(0);
		private static Dictionary<string, Function> functions;
		private static Stack<Dictionary<string, Function>> stack; // стек для хранения системных функций языка
		
		static Functions() {
			functions = new Dictionary<string, Function>();
			stack = new Stack<Dictionary<string, Function>>();
			
			math math__ = new math();
			math__.init();
			
			@string string__ = new @string();
			string__.init();
			
			@std std__ = new @std();
			std__.init();
			
			@io io__ = new @io();
			io__.init();
			
			crypto crypto__ = new crypto();
			crypto__.init();
	
			time time__ = new time();
			time__.init();
			
			algorithm alg__ = new algorithm();
			alg__.init();
			
			linq linq__ = new linq();
			linq__.init();

			functions.Add("match", new std_RegexMatch());
			functions.Add("matches", new std_RegexMatches());
			functions.Add("replaceAll", new std_ReplaceAll());
			Push();
		}
		
		public static string[] GetAllNameFunctions() {
			var collection = functions.Keys.ToArray();
			return collection;
		}
		
		// Положить переменные в стек
		public static void Push() {
			// копируем значения переменных в стек
			stack.Push(new Dictionary<string, Function>(functions));
		}
		
		// Извлечь переменные из стека
		public static void Pop() {
			functions = stack.Pop();
		}
		
		public static bool isExists(string key) {
			return functions.ContainsKey(key);
		}
		
		public static Function get(string key) {
			if (!isExists(key)) throw new  Exception("Unknown function " + key);
			Function value;
			functions.TryGetValue(key, out value);
			return value;
		}
		
		public static void set(string key, Function function) {
			functions.Add(key, function);
		}
		
		public static void assign(string key, Function function) {
			if (functions.ContainsKey(key)) {
				functions[key] = function;
			}	
		}
		
		public static void remove(string key) {
			functions.Remove(key);
		}
		
		public static void Clear() {
			functions.Clear();
			Pop();
			Push();
		}
	}
	
	public class FunctionalExpression : Expression {
		private string name;
		private List<Expression> arguments;
		// Список переменных используемых только в функции(после вызова очищать список)
		private List<string> functionVariables;
		private Stack<List<string>> stack;
		
		public FunctionalExpression(string name) {
			this.name = name;
			arguments = new List<Expression>();
			stack = new Stack<List<string>>();
			this.functionVariables = new List<string>();
		}
		
		public FunctionalExpression(string name, List<Expression> arguments) {
			this.name = name;
			this.arguments = arguments;
		}
		
		public void Push() {
			stack.Push(functionVariables);
		}
		
		public void Pop() {
			functionVariables = stack.Pop();
		}
		
		public void AddArgument(Expression arg) {
			arguments.Add(arg);
		}
		
		public Value eval() {
			int size = arguments.Count;
			Value[] values = new Value[size];
			
			for (int i = 0; i < size; i++) {
				values[i] = arguments.Get(i).eval();
			}
			
			Function function = Functions.get(name);
			
			if (function is UserDefinedFunction) {
				UserDefinedFunction userFunction = (UserDefinedFunction) function;
				if (size != userFunction.getArgsCount()) throw new Exception(String.Format("Неверное количество аргументов в функции. Ожидалось {0} аргументов", userFunction.arguments.size()));
				
				for (int i = 0; i < size; i++) {
					string argName = userFunction.getArgsName(i);
					
					if (!Variables.isExists(argName)) {
						Variables.set(argName, values[i]);
						functionVariables.Add(argName); // переменные объявленные только в функции
					} else {
						Variables.assign(argName, values[i]);
					}
					functionVariables.Clear();
				}
				Value result = userFunction.execute(values);
				
				for (int i = 0; i < functionVariables.Count; i++) {
					Variables.remove(functionVariables[i]);
				}
				
				return result;
			}
			return function.execute(values);
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return name + "( " + arguments + ")";
		} 
	}
	
	public class FunctionValue : Value {
		private Function value;
		
		public FunctionValue(Function value) {
        	this.value = value;
    	}
		
		public FunctionValue(string value) {
			this.value = Functions.get(value);
    	}
		
		public Types type() {
        	return Types.FUNCTION;
    	}
    
    	public Object raw() {
        	return value;
    	}
		
		public int asInt() { throw new InvalidCastException("Cannot cast function to integer"); }
		public ushort asUShort() { throw new InvalidCastException("Cannot cast function to ushort"); }
		public byte asByte() { throw new InvalidCastException("Cannot cast function to byte"); }
		public double asDouble() { throw new InvalidCastException("Cannot cast function to double"); }
		public long asLong() { throw new InvalidCastException("Cannot cast function to long"); }
		public char asChar() { throw new InvalidCastException("Cannot cast function to char"); }
		public bool asBool() { throw new InvalidCastException("Cannot cast function to bool"); }
    
    	public String asString() {
        	return value.ToString();
    	}
		
		public Function getValue() {
        	return value;
    	}
		
		public String toString() {
        	return asString();
    	}
	}
	
	public class FunctionStatement : Statement {
		private FunctionalExpression function;
		
		public FunctionStatement(FunctionalExpression function) {
			this.function = function;
		}
		
		public void execute() {
			function.eval();
		}
		
		public override string ToString()
		{
			return function.ToString();
		} 
	}
	#endregion
	
	public class ExitStatement : Statement {
		public void execute() {
			throw new Exception("");
		}
	}
	//===================== ARRAYS ====================//
	// Тип Array
	public class ArrayValue : IEnumerable, IEnumerator, Value {
		private Value[] elements;
		
		public ArrayValue(int size) {
			this.elements = new Value[size];
		}
		
		public ArrayValue(Value[] elements) {
			this.elements = new Value[elements.Length];
			Array.Copy(elements, this.elements, elements.Length);
		}
		
		public ArrayValue(string str) {
			this.elements = new Value[str.Length];
			
			for (int i = 0; i < str.Length; i++) {
				this.elements[i] = new StringValue(str[i]);
			}
		}
		
		public ArrayValue(ArrayValue array) {
			this.elements = array.elements;
		}

		private int position = -1;
		
		#region IEnumerable и IEnumerator implementation
		public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }
		
		public bool MoveNext() {
			if (position < elements.Length - 1) {
				position++;
                return true;
			}
			((IEnumerator)this).Reset();
            return false;
		}
		
		public void Reset() {
			position = -1;
		}
		
		public object Current
        {
            get { return elements[position]; }
        }
		#endregion		
		
		public Value[] getCopyElements() {
			Value[] result = new Value[elements.Length];
			
			for (int i = 0, len = elements.Length; i < len; i++) {
				result.SetValue(elements[i], i);
			}
			
			return result;
		}
		
		public Types type() {
			return Types.ARRAY;
		}
		
		public int asInt() { throw new InvalidCastException("Cannot cast array to int"); }
		public byte asByte() { throw new InvalidCastException("Cannot cast array to byte"); }
		public char asChar() { throw new InvalidCastException("Cannot cast array to char"); }		
		public ushort asUShort() { throw new InvalidCastException("Cannot cast array to ushort"); }
		public long asLong() { throw new InvalidCastException("Cannot cast array to long"); }
		public bool asBool() { throw new InvalidCastException("Cannot cast array to bool"); }
		public double asDouble() { throw new Exception("Cannot cast array to double"); }
		
		public int length() {
			return elements.Length;
		}
		
		public Value getValue(int index) {
			return elements[index];
		}
		
		public void setValue(int index, Value value) {
			elements[index] = value;
		}
		
		public string asString() {
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			
			for (int i = 0; i < elements.Length; i++) {
				if (i == elements.Length - 1) {
					sb.Append(elements[i]);
					break;
				}
				sb.Append(elements[i] + ", ");
			}
			
			sb.Append(" ]");
			return sb.ToString();
		}
		
		public override string ToString()
		{
			return asString();
		}
	}
	// Реализует присваивание значения элементу массива
	public class ArrayAssignmentStatement : Statement {
		private ArrayAccessExpression array;
		private Expression expression; // присваиваемое значение
		
		public ArrayAssignmentStatement(ArrayAccessExpression array, Expression expression) {
			this.array = array;
			this.expression = expression;
		}
		// arr[4][5][1] = ...
		// Получение значения для элемента многомерного массива
		public void execute() {
			array.getArray().setValue(array.lastIndex(), expression.eval());
		}
		
		public override string ToString()
		{
			return String.Format("{0} = {1}", array, expression);
		} 
	}
	// Реализует доступ к элементу массива
	public class ArrayAccessExpression : Expression {
		private String variable;
		private List<Expression> indices;
		
		public ArrayAccessExpression(String variable, List<Expression> indices) {
			this.variable = variable;
			this.indices = indices;
		}
		
		// arr[4][1]
		// arr4 = arr[4] - получение массива
		// res = arr4[1] - получение элемента от массива
		// [1] - последний элемент может любым значением(не обязательно массивом)(lastIndex)
		public Value eval() {
			return getArray().getValue(lastIndex());
		}
		
		public ArrayValue getArray() {
			ArrayValue array = consumeArray(Variables.get(variable));
			int last = indices.Count - 1; // последний индекс
			
			for (int i = 0; i < last; i++) {
				array = consumeArray(array.getValue(index(i)));
			}
			return array;
		}
		
		public int lastIndex() {
			return index(indices.Count - 1);
		}
		
		private int index(int index) {
			return (int) indices.Get(index).eval().asDouble();
		}
		
		// Проверяем и приводим к типу ArrayValue
		private ArrayValue consumeArray(Value value) {
			if (value is ArrayValue) {
				return (ArrayValue) value;
			} else {
				throw new Exception("Array expected");
			}
		}
		
		public override string ToString()
		{
			return variable + indices;
		}
	}
	// Реализуем синтаксическую конструкцию вида: let arr = [1, 2, 3, 4]
	public class ArrayExpression : Expression {
		private List<Expression> elements;

		public ArrayExpression(List<Expression> elements) {
			this.elements = elements;
		}
		
		public Value eval() {
			int size = elements.Count;
			ArrayValue array = new ArrayValue(size);
			
			for (int i = 0; i < size; i++) {
				array.setValue(i, elements.Get(i).eval());
			}
			return array;
		}
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return elements.ToString();
		} 
	}
	
	// Загружает исходный код из указанного файла
	public static class SourceLoader {
		public static string ReadSource(string filename) {
			string sourceText = String.Empty;
			
			using (StreamReader sr = new StreamReader(filename, Encoding.Default)) {
				sourceText = sr.ReadToEnd();
			}
			
			return sourceText;
		}
	}
	
	public class IncludeStatement : Statement {
		private Expression expression;
		
		public IncludeStatement(Expression expression) {
			this.expression = expression;
		}
		
		public void execute() {
			try {
				Statement program = loadProgram(expression.eval().asString());
				
				if (program != null) {
					program.execute();
				}
			} catch(Exception) {
				throw new Exception("Error include file");
			}
		}
		
		public Statement loadProgram(String path) {
	        String input = SourceLoader.ReadSource(path);
	        List<Token> tokens = new Lexer(input).tokenize();
	        Parser parser = new Parser(tokens);
	        Statement program = parser.parse();
	        return program;
	    }
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "include " + expression;
		} 
	}
	
	public class UsingStatement : Statement {
		// Путь до папки компилятора. В нее можно добавлять библиотеки на L#
		private const string SYSTEM_PATH_COMPILER = @"C:\Users\fdshfgas\Documents\_USB_\Разработки 2020\App 2020\Компилятор V3.1.1.0\LSCI V3.5.0.0\lib\";
		private Expression expression;
		
		public UsingStatement(Expression expression) {
			this.expression = expression;
		}
		
		public void execute() {
			try {
				Statement program = loadProgram(expression.eval().asString());
				
				if (program != null) {
					program.execute();
				}
			} catch(Exception) {
				throw new Exception("Error include file");
			}
		}
		
		public Statement loadProgram(String libName) {
			string path = SYSTEM_PATH_COMPILER + libName + ".txt";
	        String input = SourceLoader.ReadSource(path);
	        List<Token> tokens = new Lexer(input).tokenize();
	        Parser parser = new Parser(tokens);
	        Statement program = parser.parse();
	        return program;
	    }
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override string ToString()
		{
			return "using " + expression;
		} 
	}
	
	public class CastExpression : Expression {
		private string casttype;
		private Expression expression;
		private bool isChecked; // проверять ли числа на переполнение
		
		public CastExpression(string casttype, Expression expression, bool isChecked = true) {
			this.casttype = casttype;
			this.expression = expression;
			this.isChecked = isChecked;
		}
		
		public Value eval() {
			Value value = expression.eval();
			
			if (isChecked) {
				switch(casttype) {
					case "INT": return new NumberValue(expression.eval().asInt());
					case "LONG": return new NumberValue(expression.eval().asLong());
					case "CHAR": return new StringValue(expression.eval().asChar());
					case "STRING": return new StringValue(expression.eval().asString());
					case "BYTE": return new NumberValue(expression.eval().asByte());
					case "USHORT": return new NumberValue(expression.eval().asUShort());
					case "DOUBLE": return new NumberValue(expression.eval().asDouble());
					default:
						throw new InvalidCastException("Invalid cast operator");
				}
			}
				
			if (!isChecked) {
				switch(casttype) {
					case "INT": {
					    try { return new NumberValue(value.asInt()); }
					    catch(Exception) { return new NumberValue(int.MaxValue); }
					}
					case "LONG": {
						try { return new NumberValue(value.asLong()); }
					    catch(Exception) { return new NumberValue(long.MaxValue); }
					}
					case "BYTE": {
						try { return new NumberValue(value.asByte()); }
					    catch(Exception) { return new NumberValue(byte.MaxValue); }
					}
					case "USHORT": {
						try { return new NumberValue(value.asUShort()); }
					    catch(Exception) { return new NumberValue(ushort.MaxValue); }
					}
					case "DOUBLE": {
						try { return new NumberValue(value.asDouble()); }
					    catch(Exception) { return new NumberValue(double.MaxValue); }
					}
				default:
					throw new InvalidCastException("Invalid cast operator");
				}
			}
			throw new InvalidCastException("Invalid cast operator");
		}
	}
	// ADDED 31 Jan 2020
	public class DestructuringAssignmentStatement : Statement {
	    public List<String> variables;
	    public Expression containerExpression;
	
	    public DestructuringAssignmentStatement(List<String> arguments, Expression container) {
	        this.variables = arguments;
	        this.containerExpression = container;
	    }
	    
	    public void execute() {
	        Value container = containerExpression.eval();
	        switch (container.type()) {
	            case Types.ARRAY:
	                execute((ArrayValue) container);
	                break;   
	        }
	    }
	    
	    private void execute(ArrayValue array) {
	        int size = variables.Count;
	        for (int i = 0; i < size; i++) {
	            String variable = variables.Get(i);
	            if (variable != null) {
	            	if (!Variables.isExists(variable)) {
	            		Variables.set(variable, NumberValue.ZERO);
	            	}
	            	
	                Variables.assign(variable, array.getValue(i));
	            }
	        }
	    }
	    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	}
	
	// ADDED 1 Feb 2020
	public interface Accessible {
    	Value get();
    	Value set(Value value);
	}
	
	public class AssignmentExpression : Expression, Statement {
		public Accessible target;
	    public Operator operation;
	    public Expression expression;
	    
	    public AssignmentExpression(Operator operation, Accessible target, Expression expr) {
	        this.operation = operation;
	        this.target = target;
	        this.expression = expr;
	    }
	    
	    public void execute() {
	    	eval();
	    }
	    
	    public Value eval() {
	    	if (operation == null) {
            	return target.set(expression.eval());
        	}
	    	Expression expr1 = new ValueExpression(target.get().asDouble());
        	Expression expr2 = new ValueExpression(expression.eval().asDouble());
        	return target.set(new BinaryExpression(operation, expr1, expr2).eval());
	    }
	    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	    
	    public String toString() {
        	String op = (operation == null) ? "" : operation.ToString();
        	return String.Format("{0} {1} = {2}", target, op, expression);
	    }
	}
	
	public class FunctionDefineStatement : Statement {
		public String name;
	    public Arguments arguments;
	    public Statement body;
		
	   public FunctionDefineStatement(String name, Arguments arguments, Statement body) {
	        this.name = name;
	        this.arguments = arguments;
	        this.body = body;
    	}
	
	    public void execute() {
	    	if (!Functions.isExists(name)) {
	    		Functions.set(name, new UserDefinedFunction(arguments, body));
	    	} else {
	    		throw new IOException(String.Format("Функция {0} уже была объявлена", name));
	    	}
	    }
	    
	    public override String ToString() {
	        if (body is ReturnStatement) {
	            return String.Format("def {0}{1} = {2}", name, arguments, ((ReturnStatement)body).expression);
	        }
	        return String.Format("def {0}{1} {2}", name, arguments, body);
	    }
	}
	
	// ADDED 02 Feb 2020
	public class MapValue : IEnumerable, Value {
	    public static MapValue EMPTY = new MapValue(1);
	    private Dictionary<Value, Value> map;
	
	    public MapValue(int size) {
	        this.map = new Dictionary<Value, Value>(size);
	    }
	
	    public MapValue(Dictionary<Value, Value> map) {
	        this.map = map;
	    }

		#region IEnumerable и IEnumerator implementation
		int position = -1;
		
		public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }
		
		public bool MoveNext() {
			if (position < map.Count - 1) {
				position++;
                return true;
			}
			((IEnumerator)this).Reset();
            return false;
		}
		
		public void Reset() {
			position = -1;
		}
		#endregion		
	
	    public ArrayValue toPairs() {
	        int size = map.Count;
	        ArrayValue result = new ArrayValue(size);
	        int index = 0;
	        foreach (var entry in map) {
	            result.setValue(index++, new ArrayValue(new Value[] { entry.Key, entry.Value }));
	        }
	        return result;
	    }
	    
	    public Types type() {
	        return Types.MAP;
	    }
	    
	    public int size() {
	    	return map.Count;
	    }
	    
	    public bool containsKey(Value key) {
	        return map.ContainsKey(key);
	    }
	
	    public Value get(Value key) {
			Value value;
			map.TryGetValue(key, out value);
			return key;
	    }
	
	    public void set(String key, Value value) {
	        set(new StringValue(key), value);
	    }
	
	    public void set(String key, Function function) {
	        set(new StringValue(key), new FunctionValue(function));
	    }
	    
	    public void set(Value key, Value value) {
	        map.Add(key, value);
	    }
	
	    public Dictionary<Value, Value> getMap() {
	        return map;
	    }
	    
	    public Object raw() {
	        return map;
	    }
	    
	    public int asInt() {
	        throw new Exception("Cannot cast map to integer");
	    }
	
	    public double asDouble() {
	        throw new Exception("Cannot cast map to number");
	    }
	
		public byte asByte() { throw new Exception("Cannot cast map to byte");}
		public char asChar() { throw new Exception("Cannot cast map to char"); }
		public long asLong() { throw new Exception("Cannot cast map to long"); }
		public bool asBool() { throw new Exception("Cannot cast map to bool"); }
		public ushort asUShort() { throw new Exception("Cannot cast map to ushort"); }
	
	    public String asString() {
	        return map.ToString();
	    }
	
	    public int hashCode() {
	        int hash = 5;
	        hash = 37 * hash + this.map.GetHashCode();
	        return hash;
	    }

	    public override String ToString() {
	        return asString();
	    }
	}
	
	public class MapExpression : Expression {
    	public Dictionary<Expression, Expression> elements;

	    public MapExpression(Dictionary<Expression, Expression> arguments) {
	        this.elements = arguments;
	    }
	    
	    public Value eval() {
	        int size = elements.Count;
	        MapValue map = new MapValue(size);
	        foreach (var entry in elements) {
	        	map.set(entry.Key.eval(), entry.Value.eval());
	        }
	        return map;
	    }
	    
    	public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
    	
	    public override String ToString() {
	        StringBuilder sb = new StringBuilder();
	        // TODO
	        return sb.ToString();
	    }
	}
	
	public class ForeachMapStatement : Statement {
	    public String key, value;
	    public Expression container;
	    public Statement body;
	
	    public ForeachMapStatement(String key, String value, Expression container, Statement body) {
	        this.key = key;
	        this.value = value;
	        this.container = container;
	        this.body = body;
	    }
	
	    public void execute() {
	        Value previousVariableValue1 = Variables.isExists(key) ? Variables.get(key) : null;
	        Value previousVariableValue2 = Variables.isExists(value) ? Variables.get(value) : null;
	
	        Value containerValue = container.eval();
	        switch (containerValue.type()) {
	            case Types.STRING:
	                iterateString(containerValue.asString());
	                break;
	            case Types.ARRAY:
	                iterateArray((ArrayValue) containerValue);
	                break;
	            case Types.MAP:
	                iterateMap((MapValue) containerValue);
	                break;
	            default:
	                throw new Exception("Cannot iterate " + containerValue.type() + " as key, value pair");
	        }
	
	        // Restore variables
	        if (previousVariableValue1 != null) {
	            Variables.assign(key, previousVariableValue1);
	        } else {
	            Variables.remove(key);
	        }
	        if (previousVariableValue2 != null) {
	            Variables.assign(value, previousVariableValue2);
	        } else {
	            Variables.remove(value);
	        }
	    }
	
	    private void iterateString(String str) {
	        foreach (char ch in str.ToCharArray()) {
	            Variables.assign(key, new StringValue(ch));
	            Variables.assign(value, new NumberValue((int) ch));
	            try {
	                body.execute();
	            } catch (BreakException) {
	                break;
	            } catch (ContinueException) { }
	        }
	    }
	
	    private void iterateArray(ArrayValue containerValue) {
	        int index = 0;
	        foreach (Value v in containerValue) {
	            Variables.assign(key, v);
	            Variables.assign(value, new NumberValue(index++));
	            try {
	                body.execute();
	            } catch (BreakException) {
	                break;
	            } catch (ContinueException) { }
	        }
	    }
	
	    private void iterateMap(MapValue containerValue) {
	    	foreach (var entry in containerValue.getMap()) {
	            Variables.assign(key, entry.Key);
	            Variables.assign(value, entry.Value);
	            try {
	                body.execute();
	            } catch (BreakException) {
	                break;
	            } catch (ContinueException) { }
	        }
	    }
	
	     public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	    
	    public override String ToString() {
	        return String.Format("foreach({0}, {1} : {2}) {3}", key, value, container, body);
	    }
	}
	
	public class ContainerAccessExpression : Expression, Accessible {
	    public Expression root;
	    public List<Expression> indices;
	    private bool rootIsVariable_;
	
	    public ContainerAccessExpression(String variable, List<Expression> indices) {
	    	root = new VariableExpression(variable);
	    	this.indices = indices;
	    }
	
	    public ContainerAccessExpression(Expression root, List<Expression> indices) {
	        rootIsVariable_ = root is VariableExpression;
	        this.root = root;
	        this.indices = indices;
	    }
	
	    public bool rootIsVariable() {
	        return rootIsVariable_;
	    }
	
	    public Expression getRoot() {
	        return root;
	    }
	
	    public Value eval() {
	        return get();
	    }
	    
	    public Value get() {
	        Value container = getContainer();
	        Value lastIndex_ = lastIndex();
	        switch (container.type()) {
	            case Types.ARRAY:
	        		return ((ArrayValue) container).getValue(lastIndex_.asInt());
	
	            case Types.MAP:
	                return ((MapValue) container).get(lastIndex_);
	            /*
	            case Types.STRING:
	                return ((StringValue) container).(lastIndex);
	            case Types.CLASS:
	                return ((ClassInstanceValue) container).access(lastIndex);
	            */    
	            default:
	                throw new Exception("Array or map expected. Got " + container.type());
	        }
	    }
	
	    public Value set(Value value) {
	        Value container = getContainer();
	        Value lastIndex_ = lastIndex();
	        switch (container.type()) {
	            case Types.ARRAY:
	                int arrayIndex = lastIndex_.asInt();
	                ((ArrayValue) container).setValue(arrayIndex, value);
	                return value;
	
	            case Types.MAP:
	                ((MapValue) container).set(lastIndex_, value);
	                return value;
	                
	            default:
	                throw new Exception("Array or map expected. Got " + container.type());
	        }
	    }
	    
	    public Value getContainer() {
	        Value container = root.eval();
	        int last = indices.Count - 1;
	        for (int i = 0; i < last; i++) {
	            Value index_ = index(i);
	            switch (container.type()) {
	                case Types.ARRAY:
	                    int arrayIndex = index_.asInt();
	                    container = ((ArrayValue) container).getValue(arrayIndex);
	                    break;
	                case Types.MAP:
	                    container = ((MapValue) container).get(index_);
	                    break;
	                default:
	                    throw new Exception("Array or map expected");
	            }
	        }
	        return container;
	    }
	    
	    public Value lastIndex() {
	        return index(indices.Count - 1);
	    }
	    
	    private Value index(int index) {
	        return indices.Get(index).eval();
	    }
	    
	    public MapValue consumeMap(Value value) {
	        if (value.type() != Types.MAP) {
	            throw new Exception("Map expected");
	        }
	        return (MapValue) value;
	    }
	    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	    
	    public override String ToString() {
	    	return root + indices.ToString();
	    }
	}
	
	public class Argument {
		private string name;
		private Expression valueExpr;
		
		public Argument(string name) {
			this.name = name;
			this.valueExpr = null;
		}
		
		public Argument(string name, Expression valueExpr) {
			this.name = name;
			this.valueExpr = valueExpr;
		}
		
		public string getName() {
			return name;
		}
		
		public Expression getValueExpr() {
			return valueExpr;
		}
		
		public override String ToString() {
        	return name + (valueExpr == null ? "" : " = " + valueExpr);
    	}
	}
	
	public class Arguments {
		private List<Argument> arguments;
		private int requiredArgumentsCount;
		
		public Arguments() {
			arguments = new List<Argument>();
			requiredArgumentsCount = 0;
		}
		
		public void addRequired(String name) {
        arguments.Add(new Argument(name));
        requiredArgumentsCount++;
    }
    
	    public void addOptional(String name, Expression expr) {
	        arguments.Add(new Argument(name, expr));
	    }
	
	    public Argument get(int index) {
	        return arguments.Get(index);
	    }
	    
	    public int getRequiredArgumentsCount() {
	        return requiredArgumentsCount;
	    }
	
	    public int size() {
	        return arguments.Count;
	    }
	}

	public class FunctionReferenceExpression : Expression {
		public String name;

	    public FunctionReferenceExpression(String name) {
	        this.name = name;
	    }
	
	    public Value eval() {
	        return new FunctionValue(Functions.get(name));
	    }
		
		public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
		
		public override String ToString() {
        	return "::" + name;
    	}
	}
	
	// ADDED 07 Feb 2020
	public class ObjectCreationExpression : Expression {
		public String className;
	    public List<Expression> constructorArguments;
	
	    public ObjectCreationExpression(String className, List<Expression> constructorArguments) {
	        this.className = className;
	        this.constructorArguments = constructorArguments;
	    }
	    
	   public Value eval() {
	        ClassDeclarationStatement cd = ClassDeclarations.get(className);
	        ClassInstanceValue instance = new ClassInstanceValue(className);
	        foreach (AssignmentExpression f in cd.fields) {
	            // TODO check only variable assignments
	            String fieldName = ((VariableExpression) f.target).eval().asString();
	            instance.addField(fieldName, f.eval());
	        }
	        foreach (FunctionDefineStatement m in cd.methods) {
	            instance.addMethod(m.name, new ClassMethod(m.arguments, m.body, instance));
	        }
	        
	        // Call a constructor
	        int argsSize = constructorArguments.Count;
	        Value[] ctorArgs = new Value[argsSize];
	        for (int i = 0; i < argsSize; i++) {
	            ctorArgs[i] = constructorArguments.Get(i).eval();
	        }
	        instance.callConstructor(ctorArgs);
	        return instance;
	    }
	    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	}
	
	// Оболочка для выражений которая может использоваться как Statement
	public class ExprStatement : Expression, Statement {
	    public Expression expr;
	    
	    public ExprStatement(Expression function) {
	        this.expr = function;
	    }
	
	    public void execute() {
	        expr.eval();
	    }
	    
	    public Value eval() {
	        return expr.eval();
	    }

	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	    
	    public override String ToString() {
	        return expr.ToString();
		}
	}
	// Хранит список объявленных классов
	public static class ClassDeclarations {
		private static Dictionary<string, ClassDeclarationStatement> declarations;
		
		static ClassDeclarations() { 
			declarations = new Dictionary<string, ClassDeclarationStatement>();
		}

	    public static void clear() {
	        declarations.Clear();
	    }
		
		public static Dictionary<string, ClassDeclarationStatement> getAll() {
			return declarations;
		}
		
		public static bool isExists(String key) {
        	return declarations.ContainsKey(key);
    	}
		
		public static void assign(string key, ClassDeclarationStatement value) {
			if (declarations.ContainsKey(key)) {
				declarations[key] = value;
			}
		}
		// Удаление переменной
		public static void remove(string key) {
			declarations.Remove(key);
		}
    
	    public static ClassDeclarationStatement get(String key) {
	        if (!isExists(key)) throw new Exception(key);
	        ClassDeclarationStatement value;
	        declarations.TryGetValue(key, out value);
	        return value;
	    }
	    
	    public static void set(String key, ClassDeclarationStatement classDef) {
			if (!isExists(key)) {
	        	declarations.Add(key, classDef);
			}
	    }
	}
	
	public class ClassDeclarationStatement : Statement {
	    public String name;
	    public List<FunctionDefineStatement> methods;
	    public List<Expression> fields;
	    
	    public ClassDeclarationStatement(String name) {
	        this.name = name;
	        methods = new List<FunctionDefineStatement>();
	        fields = new  List<Expression>();
	    }
    
	    public void addField(Expression expr) {
	        fields.Add(expr);
	    }
	
	    public void addMethod(FunctionDefineStatement statement) {
	        methods.Add(statement);
	    }

	    public void execute() {
	        ClassDeclarations.set(name, this);
	    }
    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	    
	    public override String ToString() {
	        return String.Format("class {0} {\n  {1}  {2}}", name, fields, methods);
	    }
	}
	
	public class ClassMethod : UserDefinedFunction {
	    public ClassInstanceValue classInstance;
	    
	    public ClassMethod(Arguments arguments, Statement body, ClassInstanceValue classInstance) : base(arguments, body) {
	        this.classInstance = classInstance;
	    }
	
	    public Value execute(Value[] values) {
	        //Variables.push();
	        Variables.set("this", classInstance.getThisMap());
	        
	        try {
	            return base.execute(values);
	        } finally {
	            //Variables.pop();
	        }
	    }
	}
	
	public class ClassInstanceValue : Value {
	    private String className;
	    private MapValue thisMap;
	    private ClassMethod constructor;
	
	    public ClassInstanceValue(String name) {
	        this.className = name;
	        thisMap = new MapValue(10);
	    }
	
	    public MapValue getThisMap() {
	        return thisMap;
	    }
	    
	    public String getClassName() {
	        return className;
	    }
	    
	    public void addField(String name, Value value) {
	        thisMap.set(name, value);
	    }
	    
	    public void addMethod(String name, ClassMethod method) {
	        thisMap.set(name, method);
	        if (name.Equals(className)) {
	            constructor = method;
	        }
	    }
	    
	    public void callConstructor(Value[] args) {
	        if (constructor != null) {
	            constructor.execute(args);
	        }
	    }
	    
	    public Object raw() {
	        return null;
	    }
	
	    public int asInt() { throw new Exception("Cannot cast class to integer"); }
	
	    public double asDouble() { throw new Exception("Cannot cast class to integer"); }
	    
	    public byte asByte() { throw new Exception("Cannot cast class to byte"); }
	    
	    public long asLong() { throw new Exception("Cannot cast class to long"); }
	    
	    public char asChar() { throw new Exception("Cannot cast class to char"); }
	    
	    public bool asBool() { throw new Exception("Cannot cast class to bool"); }
	    
	    public ushort asUShort() { throw new Exception("Cannot cast class to ushort"); }
	
	    public String asString() {
	        return "class " + className + "@" + thisMap;
	    }
	
	    public Types type() {
	        return Types.CLASS;
	    }
	    
	    public int hashCode() {
	        int hash = 5;
	        hash = 37 * hash + GetHashCode();
	        return hash;
	    }
	
	    
	    public String toString() {
	        return asString();
	    }
	}
	// Хранит список инициализированных классов
	public class Classes {
		private static Dictionary<String, ClassInstanceValue> classes;
		static Classes(){
	        classes = new Dictionary<String, ClassInstanceValue>();
	    }
	
	    private Classes() { }
	
	    public static void clear() {
	        classes.Clear();
	    }
	
	    public static Dictionary<String, ClassInstanceValue> getFunctions() {
	        return classes;
	    }
	    
	    public static bool isExists(String key) {
	        return classes.ContainsKey(key);
	    }
	    
	    public static ClassInstanceValue get(String key) {
	        if (!isExists(key)) throw new Exception(key);
	        ClassInstanceValue value;
	        classes.TryGetValue(key, out value);
	        return value;
	    }
	    
	    public static void add(String key, ClassInstanceValue classDef) {
	        classes.Add(key, classDef);
	    }
	}
	
	//================================================//
	public abstract class Pattern {
        public Statement result;
        public Expression optCondition;

        public override String ToString() {
            StringBuilder sb = new StringBuilder();
            if (optCondition != null) {
                sb.Append(" when ").Append(optCondition);
            }
            sb.Append(": ").Append(result);
            return sb.ToString();
        }
    }
	
	public class ConstantPattern : Pattern {
        public Value constant;

        public ConstantPattern(Value pattern) {
            this.constant = pattern;
        }

        public override String ToString() {
            //return constant.ToString().Concat(super.toString());
            return ""; // FIXME
        }
    }

	public class VariablePattern : Pattern {
        public String variable;

        public VariablePattern(String pattern) {
            this.variable = pattern;
        }

        public override String ToString() {
            //return variable.Concat(super.toString());
            return ""; // FIXME
        }
    }

	public class ListPattern : Pattern {
        public List<String> parts;

        public ListPattern() {
        	parts = new List<String>();
        }

        ListPattern(List<String> parts) {
            this.parts = parts;
        }

        public void Add(String part) {
            parts.Add(part);
        }

        public override String ToString() {
            return ""; // FIXME
        }
    }
	
	public class MatchExpression : Expression, Statement {
		public Expression expression;
	    public List<Pattern> patterns;
	
	    public MatchExpression(Expression expression, List<Pattern> patterns) {
	        this.expression = expression;
	        this.patterns = patterns;
	    }
	
	    public void execute() {
	        eval();
	    }
	    
	    public Value eval() {
	        Value value = expression.eval();
	        foreach (Pattern p in patterns) {
	            if (p is ConstantPattern) {
	                ConstantPattern pattern = (ConstantPattern) p;
	                if (match(value, pattern.constant) && optMatches(p)) {
	                    return evalResult(p.result);
	                }
	            }
	            if (p is VariablePattern) {
	                VariablePattern pattern = (VariablePattern) p;
	                //if (pattern.variable.Equals("_")) return evalResult(p.result);
	
	                if (Variables.isExists(pattern.variable)) {
	                    if (match(value, Variables.get(pattern.variable)) && optMatches(p)) {
	                        return evalResult(p.result);
	                    }
	                } else {
	                    Variables.set(pattern.variable, value);
	                    if (optMatches(p)) {
	                        Value result = evalResult(p.result);
	                        Variables.remove(pattern.variable);
	                        return result;
	                    }
	                    Variables.remove(pattern.variable);
	                }
	            }
	            if ((value.type() == Types.ARRAY) && (p is ListPattern)) {
	                ListPattern pattern = (ListPattern) p;
	                if (matchListPattern((ArrayValue) value, pattern)) {
	                    // Clean up variables if matched
	                    Value result = evalResult(p.result);
	                    foreach (String var in pattern.parts) {
	                        Variables.remove(var);
	                    }
	                    return result;
	                }
	            }
	        }
	        throw new Exception("No pattern were matched");
	    }
	    
		private bool matchListPattern(ArrayValue array, ListPattern p) {
	        List<String> parts = p.parts;
	        int partsSize = parts.Count;
	        int arraySize = array.length();
	        switch (partsSize) {
	            case 0: // match [] { case []: ... }
	                if ((arraySize == 0) && optMatches(p)) {
	                    return true;
	                }
	                return false;
	
	            case 1: // match arr { case [x]: x = arr ... }
	                String variable = parts.Get(0);
	                Variables.set(variable, array);
	                if (optMatches(p)) {
	                    return true;
	                }
	                Variables.remove(variable);
	                return false;
	
	            default: { 
	                return false;
	            }
	        }
	    }
	    
	    private bool match(Value value, Value constant) {
	        if (value.type() != constant.type()) return false;
	        return value.asString() == constant.asString();
	    }
	
	    private bool optMatches(Pattern pattern) {
	        if (pattern.optCondition == null) return true;
	        return pattern.optCondition.eval() != NumberValue.ZERO;
	    }
	
	    private Value evalResult(Statement s) {
	        try {
	            s.execute();
	        } catch (ReturnStatement ret) {
	            return ret.getResult();
	        }
	        return NumberValue.ZERO;
	    }
	    
	    public void accept(Visitor visitor) {
	        visitor.visit(this);
	    }
	}
    // Хранит переменные перечисления
	public static class EnumVariables {
		static Dictionary<string, Value> variables = new Dictionary<string, Value>();
	
		public static void Add(string name, Value value) {
			variables.Add(name, value);
		}
		
		public static void Remove(string name) {
			variables.Remove(name);
		}
		
		public static Dictionary<string, Value> GetAllVariables() {
			return variables;
		}
		
		public static Value Get(string name) {
			Value value;
			variables.TryGetValue(name, out value);
			return value;
		}
		
		public static void Clear() {
			variables.Clear();
		}
	}
	// Хранит список перечислений
	public static class Enums {
		public static Dictionary<string, Dictionary<string, Value>> enums = new Dictionary<string, Dictionary<string, Value>>();
		
		public static void Add(string enumName, Dictionary<string, Value> enumValues) {
			enums.Add(enumName, enumValues);
		}
		
		public static void Remove(string enumName) {
			enums.Remove(enumName);
		}
		
		public static bool IsEnumExists(string enumName) {
			return enums.ContainsKey(enumName);
		}
		
		public static Value GetVariable(string enumName, string varName) {
			Dictionary<string, Value> values;
			enums.TryGetValue(enumName, out values);
			return values[varName];
		}
		
		public static void Clear() {
			enums.Clear();
		}
	}
	
	public class EnumStatement : Statement {
		public string enumName;
		public Dictionary<string, Value> enumerable;
		
		public EnumStatement(string enumName, Dictionary<string, Value> enumerable) {
			this.enumName = enumName;
			this.enumerable = enumerable;
			Enums.Add(enumName, enumerable);
		}
		
		public void execute() { }
		
		public override string ToString()
		{
			return String.Format("enum {0} : {1}", enumName, enumerable);
		}
	}
	
	public class VarEnumerableAssignmentStatement : Statement {
		public Dictionary<string, Value> enumerable;
		
		public VarEnumerableAssignmentStatement(Dictionary<string, Value> enumerable) {
			this.enumerable = enumerable;
			
			foreach (KeyValuePair<string, Value> variable in enumerable) {
				Variables.set(variable.Key, variable.Value);
			}
		}
		
		public void execute() { }
		
		public override string ToString()
		{
			return String.Format("const { {0} }", enumerable);
		}
	}
	
	// ADDED 14 Feb 2020
	public interface Node { 
	    void accept(Visitor visitor);
	}
	
	public interface Visitor {
	    void visit(ArrayExpression s);
	    void visit(AssignmentExpression s);
	    void visit(BinaryExpression s);
	    void visit(BlockStatement s);
	    void visit(BreakStatement s);
	    void visit(ClassDeclarationStatement s);
	    void visit(ConditionalExpression s);
	    void visit(ContainerAccessExpression s);
	    void visit(ContinueStatement s);
	    void visit(DoWhileStatement s);
	    void visit(DestructuringAssignmentStatement s);
	    void visit(ForStatement s);
	    void visit(ForeachArrayStatement s);
	    void visit(ForeachMapStatement s);
	    void visit(FunctionDefineStatement s);
	    void visit(FunctionReferenceExpression e);
	    void visit(ExprStatement s);
	    void visit(FunctionalExpression s);
	    void visit(IfStatement s);
	    void visit(IncludeStatement s);
	    void visit(MapExpression s);
	    void visit(MatchExpression s);
	    void visit(ObjectCreationExpression s);
	    void visit(PrintStatement s);
	    void visit(PrintlnStatement s);
	    void visit(ReturnStatement s);
	    void visit(TernaryExpression s);
	    void visit(UnaryExpression s);
	    void visit(ValueExpression s);
	    void visit(VariableExpression s);
	    void visit(WhileStatement st);
	    void visit(UsingStatement st);
	}
}
