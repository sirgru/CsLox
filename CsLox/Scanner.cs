using System;
using System.Collections.Generic;

namespace CsLox {
	enum TokenType {
		None,

		// Single character tokens
		LeftParen, RightParen, LeftBrace, RightBrace, Comma, Dot, Minus, Plus, Semicolon, Slash, Star,

		// One or two character tokens
		Bang, BangEqual, Equal, EqualEqual, Greater, GreaterEqual, Less, LessEqual,

		// Literals
		Identifier, String, Number,

		// Keywords
		And, Class, Else, False, Fun, For, If, Nil, Or,
		Print, Return, Super, This, True, Var, While,

		EOF
	}

	class Scanner {
		private readonly String _source;
		private List<Token> _tokens = new List<Token>();
		private List<Error> _errors = new List<Error>();
		private int _start = 0;
		private int _current = 0;
		private int _line = 1;
		private int _startColumn = 1;
		private int _currentColumn = 1;

		private Dictionary<String, TokenType> _keywords = new Dictionary<string, TokenType> {
			{ "and", TokenType.And }, 
			{ "class", TokenType.Class },
			{ "else", TokenType.Else },
			{ "false", TokenType.False },
			{ "for", TokenType.For },
			{ "fun", TokenType.Fun },
			{ "if", TokenType.If },
			{ "nil", TokenType.Nil },
			{ "or", TokenType.Or },
			{ "print", TokenType.Print },
			{ "return", TokenType.Return },
			{ "super", TokenType.Super },
			{ "this", TokenType.This },
			{ "true", TokenType.True },
			{ "var", TokenType.Var },
			{ "while", TokenType.While },
		};

		public Scanner(String source) {
			_source = source;
		}

		public ScannerResult ScanTokens() {
			while(!IsAtEnd()) {
				_start = _current;

				char c = GetCurrentCharacter();

				switch(c) {
					case '(': AddToken(TokenType.LeftParen, _currentColumn, 1); break;
					case ')': AddToken(TokenType.RightParen, _currentColumn, 1); break;
					case '{': AddToken(TokenType.LeftBrace, _currentColumn, 1); break;
					case '}': AddToken(TokenType.RightBrace, _currentColumn, 1); break;
					case ',': AddToken(TokenType.Comma, _currentColumn, 1); break;
					case '.': AddToken(TokenType.Dot, _currentColumn, 1); break;
					case '-': AddToken(TokenType.Minus, _currentColumn, 1); break;
					case '+': AddToken(TokenType.Plus, _currentColumn, 1); break;
					case ';': AddToken(TokenType.Semicolon, _currentColumn, 1); break;
					case '*': AddToken(TokenType.Star, _currentColumn, 1); break;

					case '!':
						if(CheckLookaheadCharacter('=')) {
							AddToken(TokenType.BangEqual, _currentColumn, 2);
							AdvanceCharacter();
						} else {
							AddToken(TokenType.Bang, _currentColumn, 1);
						}
						break;
					case '=':
						if(CheckLookaheadCharacter('=')) {
							AddToken(TokenType.EqualEqual, _currentColumn, 2);
							AdvanceCharacter();
						} else {
							AddToken(TokenType.Equal, _currentColumn, 1);
						}
						break;
					case '<':
						if(CheckLookaheadCharacter('=')) {
							AddToken(TokenType.LessEqual, _currentColumn, 2);
							AdvanceCharacter();
						} else {
							AddToken(TokenType.Less, _currentColumn, 1);
						}
						break;
					case '>':
						if(CheckLookaheadCharacter('=')) {
							AddToken(TokenType.GreaterEqual, _currentColumn, 2);
							AdvanceCharacter();
						} else {
							AddToken(TokenType.Greater, _currentColumn, 1);
						}
						break;
					case '/':
						if (CheckLookaheadCharacter('/')) {
							while(!IsAtEOL()) AdvanceCharacter();
						} else {
							AddToken(TokenType.Slash, _currentColumn, 1);
						}
						break;

					case ' ':
					case '\r':
					case '\t':
						// Ignore whitespace.
						break;

					case '\n': AdvanceLine(); break;

					case '"': ScanString(); break;

					default:
						if(char.IsDigit(c)) { ScanNumber(); }
						else if(char.IsLetter(c) || c == '_') { ScanIdentifierOrKeyword(); }
						else _errors.Add(new Error(_line, _line, _currentColumn, _currentColumn, ErrorType.InvalidCharacter, c.ToString())); break;
				}

				AdvanceCharacter();
			}
			_tokens.Add(new Token(TokenType.EOF, null, _line, _currentColumn , 1));
			return new ScannerResult(_tokens, _errors);
		}

		private bool IsAtEnd() {
			return _current >= _source.Length;
		}

		private bool IsAtEOL() {
			if(IsAtEnd()) return true;
			return _source[_current] == '\n';
		}

		private char GetCurrentCharacter() {
			if(IsAtEnd()) return '\0';
			return _source[_current];
		}
		private bool CheckCurrentCharacterNotAtEnd(Char expected) {
			return _source[_current] == expected;
		}

		private char GetLookaheadCharacter(int n = 1) {
			if(_current + n >= _source.Length) return '\0';
			return _source[_current + n];
		}
		private bool CheckLookaheadCharacter(char c, int n = 1) {
			return GetLookaheadCharacter(n) == c;
		}

		private void AdvanceCharacter() {
			_current++;
			_currentColumn++;
		}
		private void AdvanceLine() {
			_line++;
			_currentColumn = 0;		// Advance character will increase the start column to 1.
		}

		private void StartToken() {
			_startColumn = _currentColumn;
		}

		private void ScanString() {
			StartToken();
			AdvanceCharacter();

			while (!IsAtEndOfString()) {
				if(CheckCurrentCharacterNotAtEnd('\n')) AdvanceLine();
				AdvanceCharacter();
			}
			if(IsAtEnd()) {
				_errors.Add(new Error(_line, _line, _currentColumn - 1, _currentColumn - 1, ErrorType.UnterminatedString));
				return;
			}

			var length =  _current - _start - 1;
			String contents = _source.Substring(_start + 1, length);
			AddToken(TokenType.String, _startColumn, length, contents);
		}

		private bool IsAtEndOfString() {
			if(IsAtEnd()) return true;
			return _source[_current] == '"';
		}

		private void ScanNumber() {
			StartToken();

			while(char.IsDigit(GetLookaheadCharacter())) AdvanceCharacter();

			// Look for fractional Part
			if(CheckLookaheadCharacter('.') && char.IsDigit(GetLookaheadCharacter(2))) {
				AdvanceCharacter();  // Consume '.'.
				while(char.IsDigit(GetLookaheadCharacter())) AdvanceCharacter();
			}

			var length = _current - _start + 1;
			var number = _source.Substring(_start, length);
			AddToken(TokenType.Number, _startColumn, length, Double.Parse(number));
		}

		private void ScanIdentifierOrKeyword() {
			StartToken();

			char c = GetLookaheadCharacter();
			while(char.IsLetterOrDigit(c) || c == '_') {
				AdvanceCharacter();
				c = GetLookaheadCharacter();
			}

			var length = _current - _start + 1;
			String text = _source.Substring(_start, length);
			String value = null;

			// If it's not a keyword, make it an identifier and remember what it was
			if(!_keywords.TryGetValue(text, out TokenType tokenType)) {
				tokenType = TokenType.Identifier;
				value = text;
			} else {
				if(tokenType == TokenType.This) value = "this";
				if(tokenType == TokenType.Super) value = "super";
			}

			AddToken(tokenType, _startColumn, length, value);
		}

		private void AddToken(TokenType tokenType, int column, int length, Object literal = null) {
			_tokens.Add(new Token(tokenType, literal, _line, column, length));
		}
	}

	struct Token {
		public readonly TokenType type;
		public readonly Object value;
		public readonly int line;
		public readonly int column;
		public readonly int length;

		public Token(TokenType type, Object value, int line, int column, int length) {
			this.type = type;
			this.value = value;
			this.line = line;
			this.column = column;
			this.length = length;
		}

		public string Debug() {
			return "| " + type + " : Line : " + line + "; Column: " + column + "; Length: " + length + " " 
						+ (value != null ? "Value: " + value : "");
		}

		public string ToText() {
			return type.ToString();
		}
	}

	struct ScannerResult {
		public readonly List<Token> tokens;
		public readonly List<Error> errors;

		public ScannerResult(List<Token> tokens, List<Error> errors) {
			this.tokens = tokens;
			this.errors = errors;
		}
	}
}
