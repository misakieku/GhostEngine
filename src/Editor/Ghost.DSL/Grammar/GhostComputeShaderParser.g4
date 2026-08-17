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
    shaderModel | (definesBlock | includesBlock | keywordsBlock | hlslBlock | computeEntry)*;

shaderModel:
    SM shaderModelIdentifier SEMICOLON;

shaderModelIdentifier:
    IDENTIFIER
    | NUMBER
    | NUMBER IDENTIFIER
    | STRING_LITERAL
    ;

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
    IDENTIFIER (COMMA IDENTIFIER)* SEMICOLON;

hlslBlock:
    HLSL opaqueBracedBody;

opaqueBracedBody:
    LBRACE
        opaqueInner*
    RBRACE;

opaqueInner:
    ~(LBRACE | RBRACE)
    | opaqueBracedBody
    ;
computeEntry:
    IDENTIFIER STRING_LITERAL COLON STRING_LITERAL SEMICOLON;

functionCall:
    IDENTIFIER LPAREN functionArguments? RPAREN SEMICOLON;

functionArguments:
    functionArgument (COMMA functionArgument)*;

functionArgument:
    STRING_LITERAL | NUMBER | IDENTIFIER;
