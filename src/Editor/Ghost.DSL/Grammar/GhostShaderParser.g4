parser grammar GhostShaderParser;

options {
    tokenVocab = GhostShaderLexer;
}

// Top-level rule
shaderFile: shader + EOF;

shader:
    SHADER STRING_LITERAL (COLON STRING_LITERAL)? LBRACE
        shaderBody
    RBRACE;

shaderBody:
    shaderModel | (propertiesBlock | payloadBlock | includesBlock | pipelineBlock | hlslBlock | passBlock | functionCall)*;

shaderModel:
    SM IDENTIFIER SEMICOLON;

// Properties block
propertiesBlock:
    PROPERTIES LBRACE
        propertyStatement*
    RBRACE;

propertyStatement:
    IDENTIFIER IDENTIFIER (EQUALS propertyDefaultValue)? SEMICOLON;

propertyDefaultValue:
    IDENTIFIER LPAREN propertyDefaultArguments? RPAREN
    | NUMBER
    | STRING_LITERAL
    | IDENTIFIER;

propertyDefaultArguments:
    propertyDefaultArgument (COMMA propertyDefaultArgument)*;

propertyDefaultArgument:
    NUMBER | IDENTIFIER | STRING_LITERAL;

// Payload block
payloadBlock:
    PAYLOAD LBRACE
        payloadBody
    RBRACE;

payloadBody:
    (
        ~(LBRACE | RBRACE)
        |
        LBRACE payloadBody RBRACE
    )*;

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
    (definesBlock | includesBlock | pipelineBlock | hlslBlock | shaderEntry)*;

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
