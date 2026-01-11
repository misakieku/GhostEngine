lexer grammar GhostShaderLexer;

// Keywords
SHADER: 'shader';
PROPERTIES: 'properties';
PIPELINE: 'pipeline';
PASS: 'pass';
DEFINES: 'defines';
KEYWORDS: 'keywords';
INCLUDES: 'includes';
GLOBAL: 'global';
LOCAL: 'local';
HLSL: 'hlsl';

// Punctuation
LBRACE: '{';
RBRACE: '}';
LPAREN: '(';
RPAREN: ')';
SEMICOLON: ';';
COMMA: ',';
EQUALS: '=';
COLON: ':';

// Literals
STRING_LITERAL: '"' (~["\r\n] | '\\' .)* '"';
NUMBER: [0-9]+ ('.' [0-9]+)? | '.' [0-9]+;
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;

// Whitespace and Comments
WS: [ \t\r\n]+ -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;
