parser grammar GhostShaderParser;

options {
    tokenVocab = GhostShaderLexer;
}

// Top-level rule
shaderFile: shader+ EOF;

shader:
    SHADER STRING_LITERAL LBRACE
        shaderBody
    RBRACE;

shaderBody:
    shaderModel | (pipelineBlock | passBlock | functionCall)*;

shaderModel:
    SM IDENTIFIER SEMICOLON;

scope:
    GLOBAL | LOCAL;

// Pipeline block
pipelineBlock:
    PIPELINE LBRACE
        pipelineStatement*
    RBRACE;

pipelineStatement:
    IDENTIFIER EQUALS IDENTIFIER SEMICOLON;

// Pass block
passBlock:
    PASS STRING_LITERAL LBRACE
        passBody
    RBRACE;

// Template
passBody:
    (definesBlock | includesBlock | keywordsBlock | pipelineBlock | hlslBlock | shaderEntry)*;

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

shaderEntry:
    IDENTIFIER STRING_LITERAL COLON STRING_LITERAL SEMICOLON;

functionCall:
    IDENTIFIER LPAREN functionArguments? RPAREN SEMICOLON;

functionArguments:
    functionArgument (COMMA functionArgument)*;

functionArgument:
    STRING_LITERAL | NUMBER | IDENTIFIER;
