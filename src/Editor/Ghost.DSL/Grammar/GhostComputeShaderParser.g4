parser grammar GhostComputeShaderParser;

options {
    tokenVocab = GhostShaderLexer;
}

// Top-level rule
computeFile: compute+ EOF;

compute:
    COMPUTE STRING_LITERAL LBRACE
        computeBody
    RBRACE;

computeBody:
    shaderModel | (definesBlock | includesBlock | keywordsBlock | hlslBlock | computeEntry)*;

shaderModel:
    SM IDENTIFIER SEMICOLON;

scope:
    GLOBAL | LOCAL;

definesBlock:
    DEFINES LBRACE
        defineStatement*
    RBRACE;

defineStatement:
    IDENTIFIER SEMICOLON;

includesBlock:
    INCLUDES LBRACE
        includeStatement*
    RBRACE;

includeStatement:
    STRING_LITERAL SEMICOLON;

keywordsBlock:
    KEYWORDS LBRACE
        keywordStatement*
    RBRACE;

keywordStatement:
    scope? IDENTIFIER (COMMA IDENTIFIER)* SEMICOLON;

hlslBlock:
    HLSL LBRACE
        hlslBody
    RBRACE;

// Recursively matches content, ensuring braces are balanced.
hlslBody:
    (
        ~(LBRACE | RBRACE)   // Match ANY token except open/close braces
        | 
        LBRACE hlslBody RBRACE  // Or match a nested block recursively
    )*;

computeEntry:
    IDENTIFIER STRING_LITERAL COLON STRING_LITERAL SEMICOLON;

functionCall:
    IDENTIFIER LPAREN functionArguments? RPAREN SEMICOLON;

functionArguments:
    functionArgument (COMMA functionArgument)*;

functionArgument:
    STRING_LITERAL | NUMBER | IDENTIFIER;
