parser grammar GhostComputeShaderParser;

options {
    tokenVocab = GhostShaderLexer;
}

// Top-level rule
computeFile: compute + EOF;

compute:
    COMPUTE STRING_LITERAL LBRACE
        computeBody
    RBRACE;

computeBody:
    shaderModel | (propertiesBlock | definesBlock | includesBlock | hlslBlock | computeEntry | functionCall)*;

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

computeEntry:
    IDENTIFIER STRING_LITERAL COLON STRING_LITERAL SEMICOLON;

functionCall:
    IDENTIFIER LPAREN functionArguments? RPAREN SEMICOLON;

functionArguments:
    functionArgument (COMMA functionArgument)*;

functionArgument:
    STRING_LITERAL | NUMBER | IDENTIFIER;
