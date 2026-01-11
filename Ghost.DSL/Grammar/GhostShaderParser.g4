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
    (propertiesBlock | pipelineBlock | passBlock | functionCall)*;

// Properties block
propertiesBlock:
    PROPERTIES LBRACE
        propertyDeclaration*
    RBRACE;

propertyDeclaration:
    scope? IDENTIFIER IDENTIFIER (EQUALS LBRACE propertyInitializer RBRACE)? SEMICOLON;

scope:
    GLOBAL | LOCAL;

propertyInitializer:
    (NUMBER | IDENTIFIER) (COMMA (NUMBER | IDENTIFIER))*;

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
        hlslCode
    RBRACE;

hlslCode:
    .*? ;  // Capture everything inside hlsl block

shaderEntry:
    IDENTIFIER STRING_LITERAL COLON STRING_LITERAL SEMICOLON;

functionCall:
    IDENTIFIER LPAREN functionArguments? RPAREN SEMICOLON;

functionArguments:
    functionArgument (COMMA functionArgument)*;

functionArgument:
    STRING_LITERAL | NUMBER | IDENTIFIER;
