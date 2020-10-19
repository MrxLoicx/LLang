using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
namespace CodeStudio
{
	public enum TokenType {
	    NUMBER, 
	    HEX_NUMBER,
	    BIN_NUMBER,
	    OCT_NUMBER,
	    WORD,
	    TEXT,
	    PRINT,
	    PRINTLN,
	    IF,
	    ELSE,
	    WHILE,
	    FOR,
	    DO,
	    BREAK,
	    CONTINUE,
	    DEF,
	    RETURN,
	    LOOP,
	    INCLUDE, // Подключение файлов по полному имени
	    USING,
	    FOREACH,
	    TRY,
	    CATCH,
	    FINALLY,
	    THROW,
	    DESTRUCT,
	    SWITCH,
	    CASE,
	    WHEN,
	    CLASS,
	    NEW,
	    ENUM,
	    // CAST
	    INT,
	    LONG,
	    CHAR,
	    BYTE,
	    BOOL, 
	    STRING,
	    USHORT,
	    DOUBLE,
	    CHECKED, // при переполнении генерируется исключение
	    UNCHECKED, // исключения не возникает
	    LET, 
	    CONST, 
	    DUE, 
	    
	    PLUS,
	    MINUS,
	    INC,
	    DEC,
	    ARROW, // =>
	    POINT_WITH_COMMA, // ;
	    COLON, // :
	    DOT, // .
	    DOTDOT, // ..
	    
	    // Битовые операции
	    BITNOT,
	    BITAND,
	    BITOR,
	    BITXOR,
	    BITSHL,
	    BITSHR,
	    
	    STAR,
	    SLASH,
	    PERCENT, // %
	    POWER, // **
	    QUESTIONQUESTION, // ??
	    QUESTION, // ?
	    QUESTIONCOLON, // ?: - Elvis оператор (Н-р: res = a ?: "default")
	    EQ,
	    EQEQ,
	    EXCL, // !
	    EXCLEQ, // !=
	    LT,
	    LTEQ,
	    GT,
	    GTEQ,
	    BARBAR,
	    AMPAMP,
	    CARETCARET, // ^
	    LPAREN, // (
	    RPAREN, // )
	    LBRACKET, // [
	    RBRACKET, // ]
	    LBRACE, // {
	    RBRACE, // }
	    COMMA, // ,
	    
	    PLUSEQ, // +=
	    MINUSEQ, // -=
	    STAREQ, // *=
	    SLASHEQ, // /=
	    PERCENTEQ, // %=
	    AMPEQ, // &=
	    CARETEQ, // ^=
	    BAREQ, // |=
	    LTLTEQ, // <<=
	    GTGTEQ, // >>=
	    EOF
}

	public class Token {
	    private TokenType type;
	    private String text;
	    private int row, col;
	    
	    public Token(TokenType type, String text, int row, int col) {
	        this.type = type;
	        this.text = text;
	        this.row = row;
	        this.col = col;
	    }
	
	    public TokenType getType() {
	        return type;
	    }
	
	    public String getText() {
	        return text;
	    }
	
	    public int getRow() {
	        return row;
	    }
	
	    public int getCol() {
	        return col;
	    }
	    
	    public String position() {
	        return "[" + row + " " + col + "]";
	    }
	
	    public override string ToString() {
	    	return type + " " + position() + " " + text;
	    }
	}
	
	public class LexerException : Exception {
		private int row;
		private int col;
		private string text;
		
		public LexerException(int row, int col, string text) {
			this.row = row;
			this.col = col;
			this.text = text;
		}
		
		public void ShowMessage() {
			Program.IDE.richErrorConsole.ForeColor = Color.FromArgb(213, 106, 115);
			Program.IDE.richErrorConsole.Text = String.Format("Error: '{0}'. Строка {1}, столбец {2}", text, row, col);
		}
		
		public override string ToString()
		{
			return String.Format("Lexer Error in [row = {0}, col = {1}]: {2}", row, col, text);
		} 
	}
	
	public static class LexerErrors {
		public const string CODE_NULL = "Пустая строка";
	}
	
	public class Lexer
	{
		private const String OPERATOR_CHARS = "+-*/%()[]{}=<>!&|~^,;.?:";
		private static Dictionary<string, TokenType> OPERATORS;
		private static Dictionary<string, TokenType> KEYWORDS;
		private StringBuilder buffer;
		private String input;
	    private int length;
	    private List<Token> tokens;
	    private int pos;
	    private int row, col;
		
		public Lexer(string input) {
	    	if (String.IsNullOrWhiteSpace(input)) {
	    		LexerException lexec = new LexerException(0,0, LexerErrors.CODE_NULL);
	    		lexec.ShowMessage();
	    	}
	    	
			OPERATORS = new Dictionary<string, TokenType>();
			KEYWORDS = new Dictionary<string, TokenType>();
			buffer = new StringBuilder();
			row = col = 1;

			// INIT OPERATORS
			OPERATORS.Add("+", TokenType.PLUS);
			OPERATORS.Add("-", TokenType.MINUS);
			OPERATORS.Add("*", TokenType.STAR);
			OPERATORS.Add("/", TokenType.SLASH);
			OPERATORS.Add("%", TokenType.PERCENT);
			OPERATORS.Add("(", TokenType.LPAREN);
			OPERATORS.Add(")", TokenType.RPAREN);
			OPERATORS.Add("[", TokenType.LBRACKET);
			OPERATORS.Add("]", TokenType.RBRACKET);
			OPERATORS.Add("{", TokenType.LBRACE);
			OPERATORS.Add("}", TokenType.RBRACE);
			OPERATORS.Add("=", TokenType.EQ);
			OPERATORS.Add("<", TokenType.LT);
			OPERATORS.Add(">", TokenType.GT);
			OPERATORS.Add(",", TokenType.COMMA);
			OPERATORS.Add(";", TokenType.POINT_WITH_COMMA);
			OPERATORS.Add("!", TokenType.EXCL);
			OPERATORS.Add(":", TokenType.COLON);
			OPERATORS.Add(".", TokenType.DOT);
			OPERATORS.Add("..", TokenType.DOTDOT);
			
			OPERATORS.Add("&", TokenType.BITAND);
			OPERATORS.Add("|", TokenType.BITOR);
			OPERATORS.Add("^", TokenType.BITXOR);
			OPERATORS.Add("~", TokenType.BITNOT);
			OPERATORS.Add("<<", TokenType.BITSHL);
			OPERATORS.Add(">>", TokenType.BITSHR);
			OPERATORS.Add("==", TokenType.EQEQ);
			OPERATORS.Add("!=", TokenType.EXCLEQ);
			OPERATORS.Add("<=", TokenType.LTEQ);
			OPERATORS.Add(">=", TokenType.GTEQ);
			OPERATORS.Add("&&", TokenType.AMPAMP);
			OPERATORS.Add("||", TokenType.BARBAR);
			OPERATORS.Add("^^", TokenType.CARETCARET);
			OPERATORS.Add("++", TokenType.INC);
			OPERATORS.Add("--", TokenType.DEC);
			OPERATORS.Add("=>", TokenType.ARROW);
			OPERATORS.Add("**", TokenType.POWER);
			OPERATORS.Add("?", TokenType.QUESTION); 
			OPERATORS.Add("?:", TokenType.QUESTIONCOLON); 
			OPERATORS.Add("??", TokenType.QUESTIONQUESTION); 
			// INIT KEYWORDS
			KEYWORDS.Add("print", TokenType.PRINT);
			KEYWORDS.Add("println", TokenType.PRINTLN);
			KEYWORDS.Add("if", TokenType.IF);
			KEYWORDS.Add("else", TokenType.ELSE);
			KEYWORDS.Add("while", TokenType.WHILE);
			KEYWORDS.Add("for", TokenType.FOR);
			KEYWORDS.Add("do", TokenType.DO);
			KEYWORDS.Add("loop", TokenType.LOOP);
			KEYWORDS.Add("break", TokenType.BREAK);
			KEYWORDS.Add("continue", TokenType.CONTINUE);
			KEYWORDS.Add("def", TokenType.DEF);
			KEYWORDS.Add("return", TokenType.RETURN);
			KEYWORDS.Add("due", TokenType.DUE);
			KEYWORDS.Add("foreach", TokenType.FOREACH);
			KEYWORDS.Add("try", TokenType.TRY);
			KEYWORDS.Add("catch", TokenType.CATCH);
			KEYWORDS.Add("finally", TokenType.FINALLY);
			KEYWORDS.Add("throw", TokenType.THROW);
			KEYWORDS.Add("destruct", TokenType.DESTRUCT);
			KEYWORDS.Add("class", TokenType.CLASS);
			KEYWORDS.Add("new", TokenType.NEW);
			KEYWORDS.Add("let", TokenType.LET);
			KEYWORDS.Add("const", TokenType.CONST);
			KEYWORDS.Add("include", TokenType.INCLUDE);
			KEYWORDS.Add("using", TokenType.USING);
			KEYWORDS.Add("int", TokenType.INT);
			KEYWORDS.Add("long", TokenType.LONG);
			KEYWORDS.Add("char", TokenType.CHAR);
			KEYWORDS.Add("string", TokenType.STRING);
			KEYWORDS.Add("byte", TokenType.BYTE);
			KEYWORDS.Add("ushort", TokenType.USHORT);
			KEYWORDS.Add("double", TokenType.DOUBLE);
			KEYWORDS.Add("checked", TokenType.CHECKED);
			KEYWORDS.Add("unchecked", TokenType.UNCHECKED);
			KEYWORDS.Add("switch", TokenType.SWITCH);
			KEYWORDS.Add("case", TokenType.CASE);
			KEYWORDS.Add("when", TokenType.WHEN);
			KEYWORDS.Add("enum", TokenType.ENUM);
			
			this.input = input;
			length = input.Length;
			tokens = new List<Token>();
		}
	    
	    public List<Token> tokenize() {
	    	while (pos < length) {
	    		char current = peek(0);
	    		if (current == '0' && peek(1) == 'b') {
		    			next2();
		    			tokenizeBinNumber();
		    	} else if (current == '0' && peek(1) == 'o') {
		    			next2();
		    			tokenizeOctNumber();
		    	} else if (current == '0' && peek(1) == 'x') {
		    			next2();
		    			tokenizeHexNumber(); 
		    	} else if (Char.IsDigit(current)) tokenizeNumber();
	    		else if (isIdentifierStart(current)) tokenizeWord();
	    		else if (current == '"') tokenizeText();
	    		else if (OPERATOR_CHARS.IndexOf(current) != -1) {
	    			tokenizeOperator();
	    		} else {
	    			next(); // whitespaces
	    		}
	    	}
	    	return tokens;
	    }
	    
	    private void next2() {
		    next();
		    next();
		}
	    
	    private void tokenizeNumber() {
	        clearBuffer();
            char current = peek(0);
	        while (true) {
	            if (current == '.') {
	                if (buffer.IndexOf('.') != -1) throw new Exception("Invalid float number");
	            } else if (!Char.IsDigit(current)) {
	                break;
	            }
	            buffer.Append(current);
	            current = next();
	        }
        	addToken(TokenType.NUMBER, buffer.ToString());
	    }
	    
	    private void tokenizeWord() {
	    	clearBuffer();
	    	buffer.Append(peek(0));
	    	char current = next();
	    	
	    	while (true) {
	    		if (!isIdentifier(current)) {
	    			break;
	    		}
	    		buffer.Append(current);
	    		current = next();
	    	}
	    	
	    	string word = buffer.ToString();
	    	
	    	if (KEYWORDS.ContainsKey(word)) {
	    		TokenType tt;
	    		KEYWORDS.TryGetValue(word, out tt);
	    		addToken(tt);
	    	} else {
	    		addToken(TokenType.WORD, word);
	    	}
	    }
	    
	    private void tokenizeText() {
	    	next(); // skip "
	    	clearBuffer();
	    	char current = peek(0);
	    	
	    	while (true) {
	    		if (current == '\\') {
	    			current = next();
	    			switch (current) {
	    				case '"': current = next(); buffer.Append('"'); continue;
	                    case 'n': current = next(); buffer.Append(Environment.NewLine); continue;
	                    case 't': current = next(); buffer.Append('\t'); continue;	
	    			}
	    			buffer.Append('\\');
                	continue;
	    		}
	    		if (current == '"') break;
	    		buffer.Append(current);
	    		current = next();
	    	}
	    	next(); // skip closing "
	    	addToken(TokenType.TEXT, buffer.ToString());
	    }
	    
	    private static bool isHexNumber(char current) {
	        return "0123456789abcdef".IndexOf(Char.ToLower(current)) != -1;
	    }
		    
		private static bool IsBinNumber(char current) {
		    return "01".IndexOf(current) != -1;
		}
		    
		private static bool IsOctNumber(char current) {
		    return "01234567".IndexOf(current) != -1;
		}
		    
		private void tokenizeHexNumber() {
		    clearBuffer();
        	char current = peek(0);
        	while (isHexNumber(current) || (current == '_')) {
            	if (current != '_') {
                	buffer.Append(current);
            	}
            	current = next();
        	}
        		
        	int length = buffer.Length;
        	if (length > 0) {
            	addToken(TokenType.HEX_NUMBER, buffer.ToString());
        	}
		}
		    
		private void tokenizeBinNumber() {
		    clearBuffer();
        	char current = peek(0);
        	while (IsBinNumber(current) || (current == '_')) {
            	if (current != '_') {
                	buffer.Append(current);
            	}
            	current = next();
        	}
        		
        	int length = buffer.Length;
        	if (length > 0) {
            	addToken(TokenType.BIN_NUMBER, buffer.ToString());
        	}
		}
		    
		private void tokenizeOctNumber() {
		    clearBuffer();
        	char current = peek(0);
        	while (IsOctNumber(current) || (current == '_')) {
            	if (current != '_') {
                	buffer.Append(current);
            	}
            	current = next();
        	}
        		
        	int length = buffer.Length;
        	if (length > 0) {
            	addToken(TokenType.OCT_NUMBER, buffer.ToString());
        	}
		}
	    
	    private void tokenizeOperator() {
	    	char current = peek(0);
	    	if (current == '/') {
	    		if (peek(1) == '/') {
	    			next2();
	    			tokenizeComment();
	    			return;
	    		} else if (peek(1) == '*') {
	    			next2();
	    			tokenizeMultilineComment();
	    			return;
	    		}
	    	}
	    	clearBuffer();
	    	
	    	while (true) {
	    		string text = buffer.ToString();
	    		if (!OPERATORS.ContainsKey(text + current) && !text.IsEmpty()) {
	    			TokenType tt;
	    			OPERATORS.TryGetValue(text, out tt);
	    			addToken(tt);
	    			return;
	    		}
	    		buffer.Append(current);
	    		current = next();
	    	}
	    }
	    
	    private void tokenizeComment() {
	    	char current = peek(0);
	    	while ("\r\n\0".IndexOf(current) == -1) {
	    		current = next();
	    	}
	    }
	    
	    private void tokenizeMultilineComment() {
	    	char current = peek(0);
	    	
	    	while (true) {
	    		if (current == '\0') throw new Exception("Missing close tag");
	    		if (current == '*' && peek(1) == '/') break;
            	current = next();
	    	}
	    	next2(); // */
	    }
	    
	    private char next() {
	       ++pos;
           char result = peek(0);
            
        	if (result == '\n') {
            	++row;
            	col = 1;
        	} else ++col;
        	return result;
	    }
	    
	    // Является ли символ началом названия переменной
	    private static bool isIdentifierStart(char current) {
	    	return Char.IsLetter(current) || (current == '$') || (current == '_') || (current == '@');
	    }
	    
	    // Является ли символ частью названия переменной
	    private static bool isIdentifier(char current) {
	    	return Char.IsLetterOrDigit(current) || (current == '_')  || (current == '$') || (current == '@');
	    }
	    
	    private char peek(int relativePosition) {
	    	int position = pos + relativePosition;
	    	if (position >= length) return '\0';
	    	return input.CharAt(position);
	    }
	    
	    private void addToken(TokenType type) {
	    	addToken(type, "");
	    }
	    
	    private void addToken(TokenType type, string text) {
	    	tokens.Add(new Token(type, text, row, col));
	    }
	    
	    private void clearBuffer() {
	    	buffer.Clear();
	    }
	}
}
