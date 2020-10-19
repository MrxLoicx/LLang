using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace CodeStudio
{
	public class ParseError {
		private int line;
		private Exception exception;
		
		public ParseError(int line, Exception exception) {
        	this.line = line;
        	this.exception = exception;
    	}

    	public int getLine() {
        	return line;
    	}

   		public Exception getException() {
       		return exception;
    	}
		
		public override string ToString()
		{
			return "ParseError on line " + line + ": " + exception.Message;
		} 
	}
	
	public class ParseErrors {
		private List<ParseError> errors;

	    public ParseErrors() {
	        errors = new List<ParseError>();
	    }
	    
	    public void clear() {
	        errors.Clear();
	    }
	    
	    public void add(Exception ex, int line) {
	        errors.Add(new ParseError(line, ex));
	    }
	    
	    public bool hasErrors() {
			return !(errors.Count == 0);
	    }
		
		public String toString() {
	        StringBuilder result = new StringBuilder();
	        foreach (ParseError error in errors) {
	            result.Append(error).Append(Environment.NewLine);
	        }
	        return result.ToString();
    	}
	}
	
	public class Parser {
		private static Token EOF = new Token(TokenType.EOF, "",-1, -1);
		private Dictionary<TokenType, Operator> ASSIGN_OPERATORS;
		private ParseErrors parseErrors;
		private List<Token> tokens;
		private int pos;
		private int size;
		
		public Parser(List<Token> tokens) {
			ASSIGN_OPERATORS = new Dictionary<TokenType, Operator>();
	        ASSIGN_OPERATORS.Add(TokenType.PLUSEQ, Operator.ADD);
	        ASSIGN_OPERATORS.Add(TokenType.MINUSEQ, Operator.SUBTRACT);
	        ASSIGN_OPERATORS.Add(TokenType.STAREQ, Operator.MULTIPLY);
	        ASSIGN_OPERATORS.Add(TokenType.SLASHEQ, Operator.DIVIDE);
	        ASSIGN_OPERATORS.Add(TokenType.PERCENTEQ, Operator.REMAINDER);
	        ASSIGN_OPERATORS.Add(TokenType.AMPEQ, Operator.AND);
	        ASSIGN_OPERATORS.Add(TokenType.CARETEQ, Operator.XOR);
	        ASSIGN_OPERATORS.Add(TokenType.BAREQ, Operator.OR);
	        ASSIGN_OPERATORS.Add(TokenType.LTLTEQ, Operator.LSHIFT);
	        ASSIGN_OPERATORS.Add(TokenType.GTGTEQ, Operator.RSHIFT);
			
			this.tokens = tokens;
			this.size = tokens.Count;
		}
		
		public Statement parse() {
			BlockStatement result = new BlockStatement();
			
			while (!match(TokenType.EOF)) {
				try {
                	result.add(statement());
            	} catch (Exception ex) {
                	parseErrors.add(ex, getErrorLine());
                	recover();
            	}
			}
			
			return result;
		}
					
		private int getErrorLine() {
	        if (size == 0) return 0;
	        if (pos >= size) return tokens.Get(size - 1).getRow();
	        return tokens.Get(pos).getRow();
    	}

	    private void recover() {
	        int preRecoverPosition = pos;
	        for (int i = preRecoverPosition; i <= size; i++) {
	            pos = i;
	            try {
	                statement();
	                pos = i; // restore position
	                return;
	            } catch (Exception) {
	                // fail
	            }
	        }
	    }
		
		private Statement block() {
			BlockStatement block = new BlockStatement();
			consume(TokenType.LBRACE);
			
			while (!match(TokenType.RBRACE)) {
				block.add(statement());
			}
			return block;
		}
		
		private Statement statement() {
			if (match(TokenType.PRINT)) {
				return new PrintStatement(expression());
			}
			if (match(TokenType.PRINTLN)) {
				return new PrintlnStatement(expression());
			}
			if (match(TokenType.IF)) {
				return ifElse();
			}
			if (match(TokenType.WHILE)) {
				return whileStatement();
			}
			if (match(TokenType.DO)) {
				return doWhileStatement();
			}
			if (match(TokenType.BREAK)) {
				return new BreakStatement();
			}
			if (match(TokenType.CONTINUE)) {
				return new ContinueStatement();
			}
			if (match(TokenType.RETURN)) {
				return new ReturnStatement(expression());
			}
			if (match(TokenType.INCLUDE)) {
            	return new IncludeStatement(expression());
        	}
			if (match(TokenType.USING)) {
            	return new UsingStatement(expression());
        	}
			if (match(TokenType.ARROW)) {
				return new ReturnStatement(expression());
			}
			if (match(TokenType.FOR)) {
				return forStatement();
			}
			if (match(TokenType.FOREACH)) {
				return foreachStatement();
			}
			if (match(TokenType.LOOP)) {
				return loopStatement();
			}
			if (match(TokenType.DEF)) {
				return functionDefine();
			}
			if (match(TokenType.THROW)) {
				return new ThrowStatement(expression());
			}
			if (match(TokenType.TRY)) {
				return tryCatchFinally();
			}
			if (match(TokenType.DUE)) {
				return exitProgram();
			}
			if (match(TokenType.SWITCH)) {
				return match();
			}
			if (match(TokenType.CLASS)) {
	            return classDeclaration();
	        }
			if (match(TokenType.ENUM)) {
	            return enumDeclaration();
	        }
			if (get(0).getType() == TokenType.WORD && get(1).getType() == TokenType.LPAREN) {
				return new FunctionStatement(function());
			}
			return assignmentStatement();
		}
		
		private Statement exitProgram() {
			Token current = get(0);
			if (match(TokenType.DUE)) {
				return new ExitStatement();
			}
			throw new Exception("Unknown statement");
		}

		private Statement assignmentStatement() {
			Token current = null;
			
			if (match(TokenType.DESTRUCT)) {
				return destructuringAssignment();
			}
			
			if ((match(TokenType.CONST) || match(TokenType.LET)) && match(TokenType.LBRACE)) {
				Dictionary<string, Value> variables = new Dictionary<string, Value>();
				while(!match(TokenType.RBRACE)) {
					var varname = consume(TokenType.WORD).getText();
					consume(TokenType.EQ);
					Value value = expression().eval();
					variables.Add(varname, value);
				}
				return new VarEnumerableAssignmentStatement(variables);
			}
			
			if (match(TokenType.LET)) {
				current = get(0);
				
				if (match(TokenType.WORD) && get(0).getType() == TokenType.EQ) {
					string variable = current.getText();
					consume(TokenType.EQ);
					return new AssignmentStatement(variable, expression());
				}
				
				if (current.getType() == TokenType.WORD) {
					string variable = current.getText();
					return new AssignmentStatement(variable, null);
				}
			} 
			
			if (match(TokenType.CONST)) {
				current = get(0);
				
				if (match(TokenType.WORD) && get(0).getType() == TokenType.EQ) {
					string variable = current.getText();
					consume(TokenType.EQ);
					return new AssignmentStatement(variable, expression());
				}
			}	else {
				current = get(0);
				
				if (lookMatch(0, TokenType.WORD) && lookMatch(1, TokenType.EQ)) {
					string variable = consume(TokenType.WORD).getText();
					consume(TokenType.EQ);
					return new AssignmentStatement(variable, expression());
				}
				
				// Проверка для массивов
				if (lookMatch(0, TokenType.WORD) && lookMatch(1, TokenType.LBRACKET)) {
					ArrayAccessExpression array = element();
					consume(TokenType.EQ);
					return new ArrayAssignmentStatement(array, expression());
				}
			}
			throw new Exception("Unknown statement");
		}
		
		private DestructuringAssignmentStatement destructuringAssignment() {
	        consume(TokenType.LBRACE);
	        List<String> variables = new List<String>();
	        while (!match(TokenType.RBRACE)) {
	            if (lookMatch(0, TokenType.WORD)) {
	                variables.Add(consume(TokenType.WORD).getText());
	            } else {
	                variables.Add(null);
	            }
	            match(TokenType.COMMA);
	        }
	        consume(TokenType.EQ);
	        return new DestructuringAssignmentStatement(variables, expression());
		}
		
		 private Expression map() {
	        // {key1 : value1, key2 : value2, ...}
	        consume(TokenType.LBRACE);
	        Dictionary<Expression, Expression> elements = new  Dictionary<Expression, Expression>();
	        while (!match(TokenType.RBRACE)) {
	            Expression key = primary();
	            consume(TokenType.COLON);
	            Expression value = expression();
	            elements.Add(key, value);
	            match(TokenType.COMMA);
	        }
	        return new MapExpression(elements);
	    }
		
		// Проверяет оператор или блок операторов
		private Statement statementOrBlock() {
			if (get(0).getType() == TokenType.LBRACE) return block();
			return statement();
		}
		
		private Statement ifElse() {
			Expression condition = expression();
			Statement ifStatement = statementOrBlock();
			Statement elseStatement;
			
			if (match(TokenType.ELSE)) {
				elseStatement = statementOrBlock();
			} else {
				elseStatement = null;
			}
			return new IfStatement(condition, ifStatement, elseStatement);
		}
		
		private Statement tryCatchFinally() {
			Statement tryStatement = statementOrBlock();
			consume(TokenType.CATCH);
			Statement catchStatement = statementOrBlock();
			Statement finallyStatement;
			
			if (match(TokenType.FINALLY)) {
				finallyStatement = statementOrBlock();
			} else {
				finallyStatement = null;
			}
			return new TryCatchFinallyStatement(tryStatement, catchStatement, finallyStatement);
		}
		
		private Statement whileStatement() {
			Expression condition = expression();
			Statement statement = statementOrBlock();
			return new WhileStatement(condition, statement);	
		}
		
		private Statement doWhileStatement() {
			Statement statement = statementOrBlock();
			consume(TokenType.WHILE);
			Expression condition = expression();
			return new DoWhileStatement(condition, statement);
		}
		
		private Statement forStatement() {
			consume(TokenType.LPAREN);
			Statement initialization = assignmentStatement();
			consume(TokenType.POINT_WITH_COMMA);
			Expression termination = expression();
			consume(TokenType.POINT_WITH_COMMA);
			Statement increment = assignmentStatement();
			consume(TokenType.RPAREN);
			Statement statement = statementOrBlock();
			return new ForStatement(initialization, termination, increment, statement);
		}
		
		private Statement foreachStatement() {
			int foreachIndex = lookMatch(0, TokenType.LPAREN) ? 1 : 0;
	        if (lookMatch(foreachIndex, TokenType.WORD)
	                && lookMatch(foreachIndex + 1, TokenType.COLON)) {
	            // foreach v : arr || foreach (v : arr)
	            return foreachArrayStatement();
	        }
	        if (lookMatch(foreachIndex, TokenType.WORD)
	                && lookMatch(foreachIndex + 1, TokenType.COMMA)
	                && lookMatch(foreachIndex + 2, TokenType.WORD)
	                && lookMatch(foreachIndex + 3, TokenType.COLON)) {
	            // foreach key, value : arr || foreach (key, value : arr)
	            return foreachMapStatement();
			} else {
				throw new Exception("Invalid foreach syntax");
			}
		}
		
		private ForeachArrayStatement foreachArrayStatement() {
			// foreach x : arr
        	bool optParentheses = match(TokenType.LPAREN);
        	String variable = consume(TokenType.WORD).getText();
        	consume(TokenType.COLON);
        	Expression container = expression();
        	if (optParentheses) {
            	consume(TokenType.RPAREN);
        	}
        	Statement statement = statementOrBlock();
        	return new ForeachArrayStatement(variable, container, statement);
		}
		
		private ForeachMapStatement foreachMapStatement() {
	        // foreach k, v : map
	        bool optParentheses = match(TokenType.LPAREN);
	        String key = consume(TokenType.WORD).getText();
	        consume(TokenType.COMMA);
	        String value = consume(TokenType.WORD).getText();
	        consume(TokenType.COLON);
	        Expression container = expression();
	        if (optParentheses) {
	            consume(TokenType.RPAREN);
	        }
	        Statement statement = statementOrBlock();
	        return new ForeachMapStatement(key, value, container, statement);
    	}
		
		private Statement loopStatement() {
			Expression count = expression();
			Statement statement = statementOrBlock();
			return new LoopStatement(count, statement);
		}
		
		private EnumStatement enumDeclaration() {
			string enumName = consume(TokenType.WORD).getText();
			consume(TokenType.LBRACE);
			
			while(!match(TokenType.RBRACE)) {
				var varname = consume(TokenType.WORD).getText();
				consume(TokenType.EQ);
				Value value = expression().eval();
				EnumVariables.Add(varname, value);
			}
			return new EnumStatement(enumName, EnumVariables.GetAllVariables());
		}
		
		// Пользовательские функции
		private FunctionDefineStatement functionDefine() {
			string name = consume(TokenType.WORD).getText();
			consume(TokenType.LPAREN);
			Arguments argNames = new Arguments();
			
			// добавление аргументов для функции
			while (!match(TokenType.RPAREN)) {
				argNames.addRequired(consume(TokenType.WORD).getText());
				match(TokenType.COMMA);
			}
			Statement body = statementOrBlock();
			return new FunctionDefineStatement(name, argNames, body);
		}
		
		private FunctionalExpression function() {
			string name = consume(TokenType.WORD).getText();
			consume(TokenType.LPAREN);
			FunctionalExpression function = new FunctionalExpression(name);
			
			// добавление аргументов для функции
			while (!match(TokenType.RPAREN)) {
				function.AddArgument(expression());
				match(TokenType.COMMA);
			}
			return function;
		}
		// Нужен для синтаксиса вида: let arr = [1, 2, 3]
		private Expression array() {
			consume(TokenType.LBRACKET);
			List<Expression> elements = new List<Expression>();
			
			while (!match(TokenType.RBRACKET)) {
				elements.Add(expression());
				match(TokenType.COMMA);
			}
			return new ArrayExpression(elements);
		}
		
		// Получение элемента массива
		// Пример: let arr = arr[0]
		private ArrayAccessExpression element() {
			string variable = consume(TokenType.WORD).getText();
			// массив индексов
			List<Expression> indices = new List<Expression>();
			
			// для многомерных массивов
			do {
			    consume(TokenType.LBRACKET);
			    indices.Add(expression());
				consume(TokenType.RBRACKET);
			} while (lookMatch(0, TokenType.LBRACKET));
			return new ArrayAccessExpression(variable, indices);
		}
		
		private MatchExpression match() {
	        // switch expression {
	        //   case pattern1: result1
	        //   case pattern2 when extr: result2
	        // }
	        Expression expr = expression();
	        consume(TokenType.LBRACE);
	        List<Pattern> patterns = new List<Pattern>();
	        do {
	        	Pattern pattern = null;
	        	Token current = null;
	        
	            consume(TokenType.CASE);
	            current = get(0);
	            if (match(TokenType.NUMBER)) {
	                // case 0.5:
	                pattern = new ConstantPattern(new NumberValue(Double.Parse(current.getText(), CultureInfo.InvariantCulture)));
	            } else if (match(TokenType.HEX_NUMBER)) {
	                // case #FF:
	                pattern = new ConstantPattern(new NumberValue(Convert.ToInt64(current.getText(),16)));
	            } else if (match(TokenType.BIN_NUMBER)) {
	                // case #FF:
	                pattern = new ConstantPattern(new NumberValue(Convert.ToInt64(current.getText(),2)));
	            } else if (match(TokenType.OCT_NUMBER)) {
	                // case #FF:
	                pattern = new ConstantPattern(new NumberValue(Convert.ToInt64(current.getText(),8)));
	            } else if (match(TokenType.TEXT)) {
	                // case "text":
	                pattern = new ConstantPattern(new StringValue(current.getText()));
	            } else if (match(TokenType.WORD)) {
	                // case value:
	                pattern = new VariablePattern(current.getText());
	            }
	
	            if (pattern == null) {
	                throw new Exception("Wrong pattern in match expression: " + current);
	            }
	            
	            if (match(TokenType.WHEN)) {
	                // case e when e > 0:
	                pattern.optCondition = expression();
	            }
	
	            consume(TokenType.COLON);
	            if (match(TokenType.LBRACE)) {
	                pattern.result = block();
	            } else {
	                pattern.result = new ReturnStatement(expression());
	            }
	            patterns.Add(pattern);
	        } while (!match(TokenType.RBRACE));
	        return new MatchExpression(expr, patterns);
	    }
		
		private Statement classDeclaration() {
			String name = consume(TokenType.WORD).getText();
            ClassDeclarationStatement classDeclaration = new ClassDeclarationStatement(name);
            
            consume(TokenType.LBRACE);
	        do {
	            if (match(TokenType.DEF)) {
	                classDeclaration.addMethod(functionDefine());
	            } else {
	                Expression fieldDeclaration = null; // FIXME
	                if (fieldDeclaration != null) {
	                    classDeclaration.addField(fieldDeclaration);
	                } else {
	                    throw new Exception("Class can contain only assignments and function declarations");
	                }
	            }
	        } while (!match(TokenType.RBRACE));
	        return classDeclaration;
		}
		
		private Expression expression() {
			return ternary();
		}
		// Тернарный оператор: res = () ? true : false
		private Expression ternary() {
			Expression result = nullCoalesce();

        	if (match(TokenType.QUESTION)) {
	            Expression trueExpr = expression();
	            consume(TokenType.COLON);
	            Expression falseExpr = expression();
	            return new TernaryExpression(result, trueExpr, falseExpr);
        	}
			
			if (match(TokenType.QUESTIONCOLON)) {
            	return new BinaryExpression(Operator.ELVIS, result, expression());
        	}
			return result;
		}
		
		private Expression nullCoalesce() {
			Expression result = logicalOr();

       	 	while (true) {
            	if (match(TokenType.QUESTIONQUESTION)) {
                	result = new ConditionalExpression(Operator.NULL_COALESCE, result, expression());
                	continue;
            	}
           	 	break;
        	}
        	return result;
		}
		
		private Expression logicalOr() {
			Expression result = logicalAnd();
        
        	while (true) {
	            if (match(TokenType.BARBAR)) {
	                result = new ConditionalExpression(Operator.OR, result, logicalAnd());
	                continue;
	            }
	            break;
        	}
        	return result;
		}
		
		private Expression logicalAnd() {
			Expression result = equality();
	        
	         while (true) {
	            if (match(TokenType.AMPAMP)) {
	                result = new ConditionalExpression(Operator.AND, result, equality());
	                continue;
	            }
	            break;
			}
			return result;
		}
		
		private Expression equality() {
			Expression result = conditional();
        
	        if (match(TokenType.EQEQ)) {
	            return new ConditionalExpression(Operator.EQUALS, result, conditional());
	        }
	        if (match(TokenType.EXCLEQ)) {
	            return new ConditionalExpression(Operator.NOT_EQUALS, result, conditional());
	        }
	        return result;
		}
		
		private Expression conditional() {
			Expression result = additive();
        
	        while (true) {
	            if (match(TokenType.LT)) {
	                result = new ConditionalExpression(Operator.LT, result, additive());
	                continue;
	            }
	            if (match(TokenType.LTEQ)) {
	                result = new ConditionalExpression(Operator.LTEQ, result, additive());
	                continue;
	            }
	            if (match(TokenType.GT)) {
	                result = new ConditionalExpression(Operator.GT, result, additive());
	                continue;
	            }
	            if (match(TokenType.GTEQ)) {
	                result = new ConditionalExpression(Operator.GTEQ, result, additive());
	                continue;
	            }
	            break;
	        }
	        return result;
		}
		
		private Expression additive() {
			Expression result = multiplicative();
			
			while (true) {	
				if (match(TokenType.PLUS)) {
					result = new BinaryExpression(Operator.ADD, result, multiplicative());
					continue;
				}
				if (match(TokenType.MINUS)) {
					result = new BinaryExpression(Operator.SUBTRACT, result, multiplicative());
					continue;
				}
				if (match(TokenType.BITOR)) {
					result = new BinaryExpression(Operator.BITOR, result, multiplicative());
					continue;
				}
				if (match(TokenType.BITXOR)) {
					result = new BinaryExpression(Operator.BITXOR, result, multiplicative());
					continue;
				}
				break;
			}
			return result;
		}
		
		private Expression multiplicative() {
			Expression result = unary();
			
			while (true) {
				if (match(TokenType.STAR)) {
					result = new BinaryExpression(Operator.MULTIPLY, result, unary());
					continue;
				}
				if (match(TokenType.SLASH)) {
					result = new BinaryExpression(Operator.DIVIDE, result, unary());
					continue;
				}
				if (match(TokenType.BITAND)) {
					result = new BinaryExpression(Operator.BITAND, result, unary());
					continue;
				}
				if (match(TokenType.BITSHL)) {
					result = new BinaryExpression(Operator.LSHIFT, result, unary());
					continue;
				}
				if (match(TokenType.BITSHR)) {
					result = new BinaryExpression(Operator.RSHIFT, result, unary());
					continue;
				}
				if (match(TokenType.PERCENT)) {
					result = new BinaryExpression(Operator.REMAINDER, result, unary());
					continue;
				}
				if (match(TokenType.POWER)) {
					result = new BinaryExpression(Operator.POWER, result, unary());
					continue;
				}
				break;
			}
			return result;
		}
		
		private Expression matchChecked() {
			Expression result = null;
			if (match(TokenType.INT)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("INT", result);
			}
			
			if (match(TokenType.LONG)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("LONG", result);
			}
			
			if (match(TokenType.CHAR)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("CHAR", result);
			}
			
			if (match(TokenType.STRING)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("STRING", result);
			}
			
			if (match(TokenType.BYTE)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("BYTE", result);
			}
			
			if (match(TokenType.USHORT)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("USHORT", result);
			}
			
			if (match(TokenType.DOUBLE)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("DOUBLE", result);
			}
			throw new InvalidCastException("Invalid cast operator");
		}
		
		private Expression matchUnchecked() {
			Expression result = null;
			if (match(TokenType.INT)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("INT", result, false);
			}
			
			if (match(TokenType.LONG)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("LONG", result, false);
			}
			if (match(TokenType.BYTE)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("BYTE", result, false);
			}
			
			if (match(TokenType.USHORT)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("USHORT", result, false);
			}
			
			if (match(TokenType.DOUBLE)) {
				consume(TokenType.RPAREN);
				 result = expression();
				return new CastExpression("DOUBLE", result, false);
			}
			throw new InvalidCastException("Invalid cast operator");
		}
		
		private Expression unary() {
			//==== CAST OPERATIONS ====//
			if (match(TokenType.CHECKED)) {
				consume(TokenType.RPAREN);
				consume(TokenType.LPAREN);
				return matchChecked();
			}
			if (match(TokenType.UNCHECKED)) {
				consume(TokenType.RPAREN);
				consume(TokenType.LPAREN);
				return matchUnchecked();
			}
			
			if (isLookMatchCastOperator()) {
				return matchChecked();
			}
			//==== END CAST OPERATIONS ====//
			if (match(TokenType.EXCL)) {
				return new UnaryExpression(Operator.ECXL, primary());
			}
			if (match(TokenType.BITNOT)) {
				return new UnaryExpression(Operator.BITNOT, primary());
			}
			if (match(TokenType.MINUS)) {
				return new UnaryExpression(Operator.SUBTRACT, primary());
			}
			if (match(TokenType.INC)) {
				return new UnaryExpression(Operator.INCREMENT_POSTFIX, primary());
			}
			if (match(TokenType.DEC)) {
				return new UnaryExpression(Operator.DECREMENT_POSTFIX, primary());
			}
			if (match(TokenType.PLUS)) {
				return primary();
			}
			return primary();
		}
		// парсинг чисел и строк
		private Expression primary() {
			Token current = get(0);	

			if (match(TokenType.SWITCH)) {
					return match();
			}	
	        if (match(TokenType.NUMBER)) {
	            return new ValueExpression(Double.Parse(current.getText(), CultureInfo.InvariantCulture));
	        }
	        if (match(TokenType.HEX_NUMBER)) {
					return new ValueExpression(Convert.ToInt64(current.getText(),16));
			}
			if (match(TokenType.BIN_NUMBER)) {
				return new ValueExpression(Convert.ToInt64(current.getText(),2));
			}
			if (match(TokenType.OCT_NUMBER)) {
				return new ValueExpression(Convert.ToInt64(current.getText(),8));
			}
	        if (lookMatch(0, TokenType.WORD) && lookMatch(1, TokenType.LPAREN)) {
	            return function();
	        }
	        if (lookMatch(0, TokenType.WORD) && lookMatch(1, TokenType.LBRACKET)) {
	            return element();
	        }
	        if (lookMatch(0, TokenType.LBRACKET)) {
	            return array();
	        }
	        if (match(TokenType.WORD)) {
				string name = null;
				name = current.getText();
				if (match(TokenType.DOT)) {
					string varName = consume(TokenType.WORD).getText();
					Value varValue = Enums.GetVariable(name, varName);
					if (Variables.isExists(varName)) {
						Variables.assign(varName, varValue);
					} else {
						Variables.set(varName, varValue);
					}
					return new VariableExpression(varName);
				}
	            return new VariableExpression(current.getText());
	        }
	        if (match(TokenType.TEXT)) {
	            return new ValueExpression(current.getText());
	        }
				
			if (lookMatch(0, TokenType.LBRACE)) {
	            return map();
	        }	
				
	        if (match(TokenType.LPAREN)) {
				Expression result = null;	
					
	            result = expression();
	            match(TokenType.RPAREN);
	            return result;
	        }
			throw new Exception("Unknown expression");
		}

		private bool isCastOperator() {
			return match(TokenType.INT) || match(TokenType.LONG) || match(TokenType.BYTE) || match(TokenType.CHAR) || match(TokenType.STRING) || lookMatch(0, TokenType.USHORT) || lookMatch(0, TokenType.DOUBLE);
		}
		
		private bool isLookMatchCastOperator() {
			return lookMatch(0, TokenType.INT) || lookMatch(0, TokenType.LONG) || lookMatch(0, TokenType.BYTE) || lookMatch(0, TokenType.CHAR) || lookMatch(0, TokenType.STRING) || lookMatch(0, TokenType.USHORT) || lookMatch(0, TokenType.DOUBLE);
		}
		
		private Token consume(TokenType type) {
			Token current = get(0);
			if (type != current.getType()) throw new Exception("Token " + current + " doesn't match " + type);
			pos++;
			return current;
		}
		
		private bool lookMatch(int pos, TokenType type) {
       		 return get(pos).getType() == type;
    	}
		
		// Равен ли текущий токен указанному
		private bool match(TokenType type) {
			Token current = get(0);
			if (type != current.getType()) return false;
			pos++;
			return true;
		}
		
		private Token get(int relativePosition) {
	    	int position = pos + relativePosition;
	    	if (position >= size) return EOF;
	    	return tokens.Get(position);
	    }
	}
}
