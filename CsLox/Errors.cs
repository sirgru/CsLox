using System;
using System.Collections.Generic;

namespace CsLox {
	public enum ErrorType {
		None,

		// Scanner
		InvalidCharacter,
		InvalidCharacters,
		UnterminatedString,

		// Parser
		UnclosedParens,
		ExpectedExpression,
		ExpectedSemicolon,
		ExpectedVariableName,
		ExpectedOpenBrace,
		ExpectedClosedBrace,
		ExpectedOpenParen,
		ExpectedClosedParen,
		ExpectedIdentifier,
		ExpectedDot,

		// Semantic
		InvalidAssignmentTarget,
		MaxArguments,
		MaxParameters,
		InitializerError,
		InvalidReturnUsage,
		InvalidThisUsage,
		InvalidSuperclass,
		InvalidUseOfSuper,

	}

	public static class ErrorTypeExtensions {
		public static string ToText(this ErrorType me) {
			switch(me) {
				case ErrorType.InvalidCharacter: return "Lexical error: Invalid character";
				case ErrorType.InvalidCharacters: return "Lexical error: Invalid characters group";
				case ErrorType.UnterminatedString: return "Lexical error: Unterminated string";

				case ErrorType.UnclosedParens: return "Parsing error: Parenthesis not closed";
				case ErrorType.ExpectedExpression: return "Parsing error: Expected expression";
				case ErrorType.ExpectedSemicolon: return "Parsing error: Expected semicolon at end of expression";
				case ErrorType.ExpectedVariableName: return "Parsing error: Expected variable name";
				case ErrorType.ExpectedOpenBrace: return "Parsing error: Expected '{'";
				case ErrorType.ExpectedClosedBrace: return "Parsing error: Expected '}'";
				case ErrorType.ExpectedOpenParen: return "Parsing error: Expected '('";
				case ErrorType.ExpectedClosedParen: return "Parsing error: Expected ')'";
				case ErrorType.ExpectedIdentifier: return "Parsing error: Expected identifier";
				case ErrorType.ExpectedDot: return "Parsing error: Expected dot";

				case ErrorType.InvalidAssignmentTarget: return "Semantic error: Invalid assignment target"; 
				case ErrorType.MaxArguments: return "Semantic error: Maximum number of arguments reached";
				case ErrorType.MaxParameters: return "Semantic error: Maximum number of parameters reached";
				case ErrorType.InitializerError: return "Semantic error: Invalid initializer";
				case ErrorType.InvalidReturnUsage: return "Semantic error: Invalid return usage";
				case ErrorType.InvalidThisUsage: return "Semantic error: Can't use 'this' in the current context";
				case ErrorType.InvalidSuperclass: return "Semantic error: Invalid superclass";
				case ErrorType.InvalidUseOfSuper: return "Semantic error: Invalid use of super";
				default:
					throw new Exception("Unhandled case.");
			}
		}
	}

	public struct Error {
		public readonly int lineStart;
		public readonly int lineEnd;
		public readonly int columnStart;
		public readonly int columnEnd;
		public readonly ErrorType errorType;
		public readonly string additionalData;

		public Error(int lineStart, int lineEnd, int columnStart, int columnEnd, ErrorType errorType, string additionalData = null) {
			this.lineStart = lineStart;
			this.lineEnd = lineEnd;
			this.columnStart = columnStart;
			this.columnEnd = columnEnd;
			this.errorType = errorType;
			this.additionalData = additionalData;
		}

		internal Error(Token token, ErrorType errorType, string additionalData = null) {
			lineStart = token.line;
			lineEnd = token.line;
			columnStart = token.column;
			columnEnd = token.column + token.length - 1;
			this.errorType = errorType;
			this.additionalData = additionalData;
		}

		public override string ToString() {
			return errorType.ToText() + (additionalData != null ? ": " + additionalData : "");
		}
	}

	public static class ErrorHelpers {
		public static List<Error> ScannerErrorsMergeInvalidCharacters(List<Error> scannerErrors) {
			List<Error> results = new List<Error>();
			for(int i = 0; i < scannerErrors.Count; i++) {
				var e = scannerErrors[i];

				if(e.errorType != ErrorType.InvalidCharacter) {
					results.Add(e);
					continue;
				}

				int continousScannerErrors = 1;
				for(int j = i + 1; j < scannerErrors.Count; j++) {
					var u = scannerErrors[j];
					if(u.errorType == ErrorType.InvalidCharacter
						&& u.lineStart == scannerErrors[j - 1].lineEnd
						&& u.columnEnd == scannerErrors[j - 1].columnStart + 1) {
						continousScannerErrors++;
					} else break;
				}

				if(continousScannerErrors > 1) {
					string additionalData = "";
					for(int j = i; j < i + continousScannerErrors; j++) {
						additionalData += scannerErrors[j].additionalData + " ";
					}
					results.Add(new Error(e.lineEnd, e.lineEnd, e.columnStart, e.columnStart + continousScannerErrors - 1, ErrorType.InvalidCharacters, additionalData));
					i += continousScannerErrors - 1;
				} else {
					results.Add(e);
				}
			}
			return results;
		}
	}
}
