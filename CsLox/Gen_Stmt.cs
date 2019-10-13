using System;
using System.Collections.Generic;

namespace CsLox {

interface Visitor_Stmt<R> {
	R Visit_BlockStmt(BlockStmt stmt);
	R Visit_ClassStmt(ClassStmt stmt);
	R Visit_ExpressionStmt(ExpressionStmt stmt);
	R Visit_FunctionStmt(FunctionStmt stmt);
	R Visit_IfStmt(IfStmt stmt);
	R Visit_PrintStmt(PrintStmt stmt);
	R Visit_ReturnStmt(ReturnStmt stmt);
	R Visit_VarStmt(VarStmt stmt);
	R Visit_WhileStmt(WhileStmt stmt);
}

abstract class Stmt {
	public abstract R Accept<R>(Visitor_Stmt<R> visitor);
}

class BlockStmt : Stmt {
	public readonly List<Stmt> statements;

	public BlockStmt (List<Stmt> statements) {
		this.statements = statements;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_BlockStmt(this);
	}
}

class ClassStmt : Stmt {
	public readonly Token name;
	public readonly VariableExpr superclass;
	public readonly List<FunctionStmt> methods;

	public ClassStmt (Token name,VariableExpr superclass,List<FunctionStmt> methods) {
		this.name = name;
		this.superclass = superclass;
		this.methods = methods;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_ClassStmt(this);
	}
}

class ExpressionStmt : Stmt {
	public readonly Expr expression;

	public ExpressionStmt (Expr expression) {
		this.expression = expression;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_ExpressionStmt(this);
	}
}

class FunctionStmt : Stmt {
	public readonly Token name;
	public readonly List<Token> parameters;
	public readonly List<Stmt> body;

	public FunctionStmt (Token name,List<Token> parameters,List<Stmt> body) {
		this.name = name;
		this.parameters = parameters;
		this.body = body;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_FunctionStmt(this);
	}
}

class IfStmt : Stmt {
	public readonly Expr condition;
	public readonly Stmt thenBranch;
	public readonly Stmt elseBranch;

	public IfStmt (Expr condition,Stmt thenBranch,Stmt elseBranch) {
		this.condition = condition;
		this.thenBranch = thenBranch;
		this.elseBranch = elseBranch;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_IfStmt(this);
	}
}

class PrintStmt : Stmt {
	public readonly Expr expression;

	public PrintStmt (Expr expression) {
		this.expression = expression;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_PrintStmt(this);
	}
}

class ReturnStmt : Stmt {
	public readonly Token keyword;
	public readonly Expr value;

	public ReturnStmt (Token keyword,Expr value) {
		this.keyword = keyword;
		this.value = value;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_ReturnStmt(this);
	}
}

class VarStmt : Stmt {
	public readonly Token name;
	public readonly Expr initializer;

	public VarStmt (Token name,Expr initializer) {
		this.name = name;
		this.initializer = initializer;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_VarStmt(this);
	}
}

class WhileStmt : Stmt {
	public readonly Expr condition;
	public readonly Stmt body;

	public WhileStmt (Expr condition,Stmt body) {
		this.condition = condition;
		this.body = body;
	}

	public override R Accept<R>(Visitor_Stmt<R> visitor) {
		return visitor.Visit_WhileStmt(this);
	}
}

}
