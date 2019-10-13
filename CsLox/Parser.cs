using System;
using System.Collections.Generic;

namespace CsLox {
	public struct ParseResult {
		internal readonly List<Stmt> statements;
		internal readonly List<Error> errors;

		internal ParseResult(List<Stmt> statements, List<Error> errors) {
			this.statements = statements;
			this.errors = errors;
		}
	}

	class Parser {
		private readonly List<Token> _tokens;

		private List<Error> _errors = new List<Error>();
		private int _current = 0;

		public Parser(List<Token> tokens) {
			_tokens = tokens;
		}

		internal ParseResult Parse() {
			List<Stmt> statements = new List<Stmt>();
			while(!IsAtEnd()) {
				statements.Add(GeneralStmt());
			}
			return new ParseResult(statements, _errors);
		}

		// Movers -----------------
		private Token GetCurrentToken() {
			return _tokens[_current];
		}
		private Token GetCurrentTokenAndAdvance() {
			Token t = _tokens[_current];
			Advance();
			return t;
		}
		private Token GetPreviousToken() {
			return _tokens[_current - 1];
		}

		private bool CheckCurrentToken(TokenType type) {
			return GetCurrentToken().type == type;
		}
		private bool CheckCurrentToken(TokenType type1, TokenType type2) {
			return GetCurrentToken().type == type1 || GetCurrentToken().type == type2;
		}
		private bool CheckCurrentToken(TokenType type1, TokenType type2, TokenType type3, TokenType type4) {
			return GetCurrentToken().type == type1 || GetCurrentToken().type == type2
				|| GetCurrentToken().type == type3 || GetCurrentToken().type == type4;
		}
		private bool CheckCurrentTokenAndAdvance(TokenType type) {
			var b = GetCurrentToken().type == type;
			if(b) Advance();
			return b;
		}

		private bool IsAtEnd() {
			return GetCurrentToken().type == TokenType.EOF;
		}

		private void Advance() {
			if(!IsAtEnd()) _current++;
		}

		private Token Expect(TokenType token, ErrorType error, string additionalData = null) {
			if(CheckCurrentToken(token)) {
				return GetCurrentTokenAndAdvance();
			} else {
				throw CreateError(GetCurrentToken(), error, additionalData);
			}
		}

		private class ParseException : Exception {}

		private ParseException CreateError(Token token, ErrorType type, string additionalData = null) {
			_errors.Add(new Error(token, type, additionalData));
			return new ParseException();
		}

		private void Synchronize() {
			Advance();

			while(!IsAtEnd()) {
				if(GetPreviousToken().type == TokenType.Semicolon) return;

				switch(GetCurrentToken().type) {
					case TokenType.Class:
					case TokenType.Fun:
					case TokenType.Var:
					case TokenType.For:
					case TokenType.If:
					case TokenType.While:
					case TokenType.Print:
					case TokenType.Return:
						return;
				}
				Advance();
			}
		}


		// Rules ------------------

		//expression → assignment ;
		private Expr Expression() {
			return Assignment();
		}

		// assignment → identifier "=" assignment | logic_or ;
		private Expr Assignment() {
			Expr expr = Or();

			if(CheckCurrentToken(TokenType.Equal)) {
				Token equalsToken = GetCurrentTokenAndAdvance();

				Expr value = Assignment();

				var nameToken = expr as VariableExpr;
				if(nameToken != null) return new AssignExpr(nameToken.name, value);

				else if(expr is GetExpr) {
					GetExpr get = (GetExpr)expr;
					return new SetExpr(get.obj, get.name, value);
				}

				CreateError(equalsToken, ErrorType.InvalidAssignmentTarget);
			}

			return expr;
		}

		// logic_or → logic_and ( "or" logic_and )* ;
		private Expr Or() {
			Expr expr = And();

			while(CheckCurrentToken(TokenType.Or)) {
				var op = GetCurrentTokenAndAdvance();
				Expr right = And();
				expr = new LogicalExpr(expr, op, right);
			}
			return expr;
		}

		// logic_and  → equality ( "and" equality )* ;
		private Expr And() {
			Expr expr = Equality();

			while(CheckCurrentToken(TokenType.And)) {
				var op = GetCurrentTokenAndAdvance();
				Expr right = Equality();
				expr = new LogicalExpr(expr, op, right);
			}
			return expr;
		}

		//equality → comparison(( "!=" | "==" ) comparison )* ;
		private Expr Equality() {
			Expr expr = Comparison();

			while(CheckCurrentToken(TokenType.BangEqual, TokenType.EqualEqual)) {
				Token op = GetCurrentTokenAndAdvance();
				Expr right = Comparison();
				expr = new BinaryExpr(expr, op, right);
			}
			return expr;
		}

		//comparison → addition(( ">" | ">=" | "<" | "<=" ) addition )* ;
		private Expr Comparison() {
			Expr expr = Addition();

			while(CheckCurrentToken(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual)) {
				Token op = GetCurrentTokenAndAdvance();
				Expr right = Addition();
				expr = new BinaryExpr(expr, op, right);
			}
			return expr;
		}

		//addition → multiplication(( "-" | "+" ) multiplication )* ;
		private Expr Addition() {
			Expr expr = Multiplication();

			while(CheckCurrentToken(TokenType.Minus, TokenType.Plus)) {
				Token op = GetCurrentTokenAndAdvance();
				Expr right = Multiplication();
				expr = new BinaryExpr(expr, op, right);
			}
			return expr;
		}

		//multiplication → unary(( "/" | "*" ) unary )* ;
		private Expr Multiplication() {
			Expr expr = Unary();

			while(CheckCurrentToken(TokenType.Slash, TokenType.Star)) {
				Token op = GetCurrentTokenAndAdvance();
				Expr right = Unary();
				expr = new BinaryExpr(expr, op, right);
			}
			return expr;
		}

		//unary → ( "!" | "-" ) unary
		//     | primary ;
		private Expr Unary() {
			if(CheckCurrentToken(TokenType.Bang, TokenType.Minus)) {
				Token op = GetCurrentTokenAndAdvance();
				Expr right = Unary();
				return new UnaryExpr(op, right);
			}
			return Call();
		}

		private Expr Call() {
			Expr expr = Primary();

			while(true) {
				if(CheckCurrentTokenAndAdvance(TokenType.LeftParen)) {
					expr = FinishCall(expr);

				} else if(CheckCurrentTokenAndAdvance(TokenType.Dot)) {
					Token name = Expect(TokenType.Identifier, ErrorType.ExpectedIdentifier);
					expr = new GetExpr(expr, name);

				} else {
					break;
				}
			}
			return expr;
		}
		private Expr FinishCall(Expr callee) {
			List<Expr> arguments = new List<Expr>();
			if(!CheckCurrentToken(TokenType.RightParen)) {
				do {
					if(arguments.Count >= 255) {
						CreateError(GetCurrentToken(), ErrorType.MaxArguments);
					}
					arguments.Add(Expression());
				} while(CheckCurrentTokenAndAdvance(TokenType.Comma));
			}

			Token paren = Expect(TokenType.RightParen, ErrorType.ExpectedClosedParen);

			return new CallExpr(callee, paren, arguments);
		}

		//primary → NUMBER | STRING | "false" | "true" | "nil"
		//        | "(" expression ")" ;
		private Expr Primary() {
			if(CheckCurrentTokenAndAdvance(TokenType.False)) return new LiteralExpr(false);

			if(CheckCurrentTokenAndAdvance(TokenType.True)) return new LiteralExpr(true);

			if(CheckCurrentTokenAndAdvance(TokenType.Nil)) return new LiteralExpr(null);

			if(CheckCurrentToken(TokenType.Number, TokenType.String)) {
				var t = GetCurrentToken();
				Advance();
				return new LiteralExpr(t.value);
			}

			if(CheckCurrentToken(TokenType.Super)) {
				Token keyword = GetCurrentToken();
				Advance();
				Expect(TokenType.Dot, ErrorType.ExpectedDot, "after 'super'");
				Token method = Expect(TokenType.Identifier, ErrorType.ExpectedIdentifier, "for superclass method name");
				return new SuperExpr(keyword, method);
			}


			if(CheckCurrentToken(TokenType.This)) {
				var t = GetCurrentToken();
				Advance();
				return new ThisExpr(t);
			}

			if(CheckCurrentToken(TokenType.Identifier)) {
				var t = GetCurrentToken();
				Advance();
				return new VariableExpr(t);
			}

			if(CheckCurrentTokenAndAdvance(TokenType.LeftParen)) {
				Expr expr = Expression();

				Expect(TokenType.RightParen, ErrorType.UnclosedParens);
				
				return new GroupingExpr(expr);
			}
			throw CreateError(GetCurrentToken(), ErrorType.ExpectedExpression);
		}

		private Stmt GeneralStmt() {
			try {
				if(CheckCurrentTokenAndAdvance(TokenType.Class)) return ClassDeclaration();
				if(CheckCurrentTokenAndAdvance(TokenType.Fun)) return Function("function");
				if(CheckCurrentTokenAndAdvance(TokenType.Var)) return VarDeclaration();

				return Statement();
			} catch(ParseException) {
				Synchronize();
				return null;
			}
		}

		private Stmt Statement() {
			if(CheckCurrentTokenAndAdvance(TokenType.For)) return ForStatement();

			if(CheckCurrentTokenAndAdvance(TokenType.If)) return IfStatement();

			if(CheckCurrentTokenAndAdvance(TokenType.Print)) return PrintStatement();

			if(CheckCurrentTokenAndAdvance(TokenType.Return)) return ReturnStatement();

			if(CheckCurrentTokenAndAdvance(TokenType.While)) return WhileStatement();

			if(CheckCurrentTokenAndAdvance(TokenType.LeftBrace)) return new BlockStmt(Block());

			return ExpressionStatement();
		}

		private Stmt ClassDeclaration() {
			Token name = Expect(TokenType.Identifier, ErrorType.ExpectedIdentifier);

			VariableExpr superclass = null;
			if(CheckCurrentTokenAndAdvance(TokenType.Less)) {
				var e = Expect(TokenType.Identifier, ErrorType.ExpectedIdentifier);
				superclass = new VariableExpr(e);
			}

			Expect(TokenType.LeftBrace, ErrorType.ExpectedOpenBrace);

			List<FunctionStmt> methods = new List<FunctionStmt>();
			while(!CheckCurrentToken(TokenType.RightBrace) && !IsAtEnd()) {
				methods.Add(Function("method"));
			}

			Expect(TokenType.RightBrace, ErrorType.ExpectedClosedBrace);

			return new ClassStmt(name, superclass, methods);
		}

		private List<Stmt> Block() {
			List<Stmt> statements = new List<Stmt>();

			while(!CheckCurrentToken(TokenType.RightBrace) && !IsAtEnd()) {
				statements.Add(GeneralStmt());
			}

			Expect(TokenType.RightBrace, ErrorType.ExpectedClosedBrace);
			return statements;
		}

		private Stmt IfStatement() {
			Expect(TokenType.LeftParen, ErrorType.ExpectedOpenParen);

			Expr condition = Expression();

			Expect(TokenType.RightParen, ErrorType.ExpectedClosedParen);

			Stmt thenBranch = Statement();

			Stmt elseBranch = null;
			if(CheckCurrentToken(TokenType.Else)) {
				Advance();
				elseBranch = Statement();
			}

			return new IfStmt(condition, thenBranch, elseBranch);
		}

		private Stmt WhileStatement() {
			Expect(TokenType.LeftParen, ErrorType.ExpectedOpenParen);
			Expr condition = Expression();
			Expect(TokenType.RightParen, ErrorType.ExpectedClosedParen);
			Stmt body = Statement();

			return new WhileStmt(condition, body);
		}

		private Stmt ForStatement() {
			Expect(TokenType.LeftParen, ErrorType.ExpectedOpenParen);

			Stmt initializer;
			if(CheckCurrentToken(TokenType.Semicolon)) {
				initializer = null;
				Advance();
			} else if(CheckCurrentToken(TokenType.Var)) {
				Advance();
				initializer = VarDeclaration();
			} else {
				initializer = ExpressionStatement();
			}

			Expr condition = null;
			if(!CheckCurrentToken(TokenType.Semicolon)) {
				condition = Expression();
			}
			Expect(TokenType.Semicolon, ErrorType.ExpectedSemicolon);

			Expr increment = null;
			if(!CheckCurrentToken(TokenType.RightParen)) {
				increment = Expression();
			}
			Expect(TokenType.RightParen, ErrorType.ExpectedClosedParen);

			Stmt body = Statement();

			if(increment != null) {
				body = new BlockStmt(new List<Stmt> { body, new ExpressionStmt(increment) });
			}

			if(condition == null) condition = new LiteralExpr(true);
			body = new WhileStmt(condition, body);

			if(initializer != null) {
				body = new BlockStmt(new List<Stmt> { initializer, body });
			}

			return body;
		}

		private Stmt VarDeclaration() {
			Token name = Expect(TokenType.Identifier, ErrorType.ExpectedVariableName);

			Expr initializer = null;
			if(CheckCurrentTokenAndAdvance(TokenType.Equal)) {
				initializer = Expression();
			}

			Expect(TokenType.Semicolon, ErrorType.ExpectedSemicolon);
			return new VarStmt(name, initializer);
		}

		private Stmt PrintStatement() {
			Expr value = Expression();
			Expect(TokenType.Semicolon, ErrorType.ExpectedSemicolon);
			return new PrintStmt(value);
		}

		private Stmt ExpressionStatement() {
			Expr expr = Expression();
			Expect(TokenType.Semicolon, ErrorType.ExpectedSemicolon);
			return new ExpressionStmt(expr);
		}

		private FunctionStmt Function(string kind) {
			Token name = Expect(TokenType.Identifier, ErrorType.ExpectedIdentifier);

			Expect(TokenType.LeftParen, ErrorType.ExpectedOpenParen);
			List<Token> parameters = new List<Token>();
			if(!CheckCurrentToken(TokenType.RightParen)) {
				do {
					if(parameters.Count >= 255) {
						CreateError(GetCurrentToken(), ErrorType.MaxArguments);
					}

					parameters.Add(Expect(TokenType.Identifier, ErrorType.ExpectedIdentifier));
				} while(CheckCurrentTokenAndAdvance(TokenType.Comma));
			}
			Expect(TokenType.RightParen, ErrorType.ExpectedClosedParen);

			Expect(TokenType.LeftBrace, ErrorType.ExpectedOpenBrace);
			List<Stmt> body = Block();
			return new FunctionStmt(name, parameters, body);
		}

		private Stmt ReturnStatement() {
			Token keyword = GetPreviousToken();
			Expr value = null;
			if(!CheckCurrentToken(TokenType.Semicolon)) {
				value = Expression();
			}
			Expect(TokenType.Semicolon, ErrorType.ExpectedSemicolon);
			return new ReturnStmt(keyword, value);
		}
	}
}
