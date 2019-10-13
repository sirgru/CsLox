using System.Collections.Generic;

namespace CsLox {
	class ScopeResolver : Visitor_Expr<Void>, Visitor_Stmt<Void> {
		private readonly Interpreter _interpreter;
		private Stack<Dictionary<string, bool>> _scopes = new Stack<Dictionary<string, bool>>();
		private FunctionType _currentFunctionType;
		private List<Error> _errors = new List<Error>();
		private ClassType _currentClass = ClassType.None;

		public ScopeResolver(Interpreter interpreter) {
			_interpreter = interpreter;
		}

		public List<Error> StartResolve(List<Stmt> statements) {
			Resolve(statements);
			return _errors;
		}

		public void Resolve(List<Stmt> statements) {
			foreach(var statement in statements) {
				Resolve(statement);
			}
		}

		public Void Visit_BlockStmt(BlockStmt stmt) {
			BeginScope();
			Resolve(stmt.statements);
			EndScope();
			return null;
		}
		
		void Resolve(Stmt stmt) {
			stmt.Accept(this);
		}
		void Resolve(Expr expr) {
			expr.Accept(this);
		}
		void BeginScope() {
			_scopes.Push(new Dictionary<string, bool>());
		}
		void EndScope() {
			_scopes.Pop();
		}

		public Void Visit_VarStmt(VarStmt stmt) {
			Declare(stmt.name);
			if(stmt.initializer != null) {
				Resolve(stmt.initializer);
			}
			Define(stmt.name);
			return null;
		}
		void Declare(Token name) {
			if(_scopes.Count == 0) return;

			Dictionary<string, bool> scope = _scopes.Peek();
			if(scope.ContainsKey((string)name.value)) {
				_errors.Add(new Error(name, ErrorType.InvalidAssignmentTarget, "Variable with this name already declared in this scope."));
			} else {
				scope.Add((string)name.value, false);
			}
		}
		void Define(Token name) {
			if(_scopes.Count == 0) return;
			_scopes.Peek()[(string)name.value] = true;
		}

		public Void Visit_VariableExpr(VariableExpr expr) {
			var name = (string)expr.name.value;
			if(_scopes.Count != 0) {
				var scope = _scopes.Peek();
				var hasKey = scope.TryGetValue(name, out bool state);
				if(hasKey && state == false) {
					_errors.Add(new Error(expr.name, ErrorType.InitializerError, "Cannot read local variable in its own initializer."));
				}
			}
			ResolveLocal(expr, expr.name);
			return null;
		}
		void ResolveLocal(Expr expr, Token name) {
			var scopesArray = _scopes.ToArray();
			for(int i = 0; i < _scopes.Count; i++) {
				if(scopesArray[i].ContainsKey((string)name.value)) {
					_interpreter.BindLocal(expr, i);
					return;
				}
			}
			// Not found. Assume it is global.                   
		}

		public Void Visit_AssignExpr(AssignExpr expr) {
			Resolve(expr.value);
			ResolveLocal(expr, expr.name);
			return null;
		}

		public Void Visit_FunctionStmt(FunctionStmt stmt) {
			Declare(stmt.name);
			Define(stmt.name);
			ResolveFunction(stmt, FunctionType.Function);
			return null;
		}

		void ResolveFunction(FunctionStmt function, FunctionType functionType) {
			FunctionType enclosingFunctionType = _currentFunctionType;
			_currentFunctionType = functionType;

			BeginScope();
			foreach(var param in function.parameters) {
				Declare(param);
				Define(param);
			}
			Resolve(function.body);
			EndScope();

			_currentFunctionType = enclosingFunctionType;
		}

		public Void Visit_ExpressionStmt(ExpressionStmt stmt) {
			Resolve(stmt.expression);
			return null;
		}

		public Void Visit_IfStmt(IfStmt stmt) {
			Resolve(stmt.condition);
			Resolve(stmt.thenBranch);
			if(stmt.elseBranch != null) Resolve(stmt.elseBranch);
			return null;
		}

		public Void Visit_PrintStmt(PrintStmt stmt) {
			Resolve(stmt.expression);
			return null;
		}

		public Void Visit_ReturnStmt(ReturnStmt stmt) {
			if(_currentFunctionType == FunctionType.None) {
				_errors.Add(new Error(stmt.keyword, ErrorType.InvalidReturnUsage, "Cannot return from top-level code."));
			}

			if(stmt.value != null) {
				if(_currentFunctionType == FunctionType.Initializer) {
					_errors.Add(new Error(stmt.keyword, ErrorType.InvalidReturnUsage, "Can't use 'return' inside a constructor"));
				}

				Resolve(stmt.value);
			}
			return null;
		}

		public Void Visit_WhileStmt(WhileStmt stmt) {
			Resolve(stmt.condition);
			Resolve(stmt.body);
			return null;
		}

		public Void Visit_BinaryExpr(BinaryExpr expr) {
			Resolve(expr.left);
			Resolve(expr.right);
			return null;
		}

		public Void Visit_CallExpr(CallExpr expr) {
			Resolve(expr.callee);
			foreach(var argument in expr.arguments) {
				Resolve(argument);
			}
			return null;
		}

		public Void Visit_GroupingExpr(GroupingExpr expr) {
			Resolve(expr.expression);
			return null;
		}

		public Void Visit_LiteralExpr(LiteralExpr expr) {
			return null;
		}

		public Void Visit_LogicalExpr(LogicalExpr expr) {
			Resolve(expr.left);
			Resolve(expr.right);
			return null;
		}

		public Void Visit_UnaryExpr(UnaryExpr expr) {
			Resolve(expr.right);
			return null;
		}

		public Void Visit_ClassStmt(ClassStmt stmt) {
			ClassType enclosingClass = _currentClass;
			_currentClass = ClassType.Class;

			Declare(stmt.name);
			Define(stmt.name);

			if(stmt.superclass != null && (string)stmt.name.value == (string)stmt.superclass.name.value) {
				_errors.Add(new Error(stmt.name, ErrorType.InvalidSuperclass, "A class cannot inherit from itself."));
			}

			if(stmt.superclass != null) {
				_currentClass = ClassType.Subclass;
				Resolve(stmt.superclass);
				BeginScope();
				_scopes.Peek().Add("super", true);
			}

			BeginScope();
			_scopes.Peek().Add("this", true);

			foreach(var method in stmt.methods) {
				var ft = FunctionType.Method;
				if((string)method.name.value == "init") {
					ft = FunctionType.Initializer;
				}
				ResolveFunction(method, ft);
			}
			EndScope();

			if(stmt.superclass != null) EndScope();

			_currentClass = enclosingClass;
			return null;
		}

		public Void Visit_GetExpr(GetExpr expr) {
			Resolve(expr.obj);
			return null;
		}

		public Void Visit_SetExpr(SetExpr expr) {
			Resolve(expr.value);
			Resolve(expr.obj);
			return null;
		}
		
		public Void Visit_ThisExpr(ThisExpr expr) {
			if(_currentClass == ClassType.None) {
				_errors.Add(new Error(expr.keyword, ErrorType.InvalidThisUsage));
				return null;
			}

			ResolveLocal(expr, expr.keyword);
			return null;
		}

		public Void Visit_SuperExpr(SuperExpr expr) {
			if(_currentClass == ClassType.None) {
				_errors.Add(new Error(expr.keyword, ErrorType.InvalidUseOfSuper, "Cannot use 'super' outside of a class."));

			} else if(_currentClass != ClassType.Subclass) {
				_errors.Add(new Error(expr.keyword, ErrorType.InvalidUseOfSuper, "Cannot use 'super' in a class wihtout superclass."));
			}
			ResolveLocal(expr, expr.keyword);
			return null;
		}
	}

	internal enum FunctionType {
		None,
		Function,
		Initializer,
		Method,
	}

	internal enum ClassType { None, Class, Subclass }

}
