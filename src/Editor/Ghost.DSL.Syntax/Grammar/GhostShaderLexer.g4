lexer grammar GhostShaderLexer;

// Keywords
SHADER: 'shader';
COMPUTE: 'compute';
PIPELINE: 'pipeline';
PASS: 'pass';
DEFINES: 'defines';
KEYWORDS: 'keywords';
INCLUDES: 'includes';
GLOBAL: 'global';
LOCAL: 'local';
HLSL: 'hlsl';
SM: 'sm';
MODULE: 'module';
IMPORT: 'import';
EXPORT: 'export';
CLOSED: 'closed';
INTERFACE: 'interface';
IMPLEMENTATION: 'implementation';
TEMPLATE: 'template';
SLOT: 'slot';
BIND: 'bind';
COMPOSE: 'compose';
PAYLOAD: 'payload';
SHADER_PROJECT: 'shader_project';
TARGET: 'target';
PROVIDER: 'provider';
PROPERTIES: 'properties';

// Punctuation
LBRACE: '{';
RBRACE: '}';
LPAREN: '(';
RPAREN: ')';
LBRACK: '[';
RBRACK: ']';
SEMICOLON: ';';
COMMA: ',';
EQUALS: '=';
COLON: ':';
DOT: '.';

// Literals
STRING_LITERAL: '"' (~["\r\n] | '\\' .)* '"';
NUMBER: [0-9]+ ('.' [0-9]+)? | '.' [0-9]+;
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;

// Whitespace and Comments
WS: [ \t\r\n]+ -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;

ANY_CHAR: . ;
