namespace Ghost.DXC;

public enum DxcCompletionChunkKind
{
    DxcCompletionChunk_Optional = 0,
    DxcCompletionChunk_TypedText = 1,
    DxcCompletionChunk_Text = 2,
    DxcCompletionChunk_Placeholder = 3,
    DxcCompletionChunk_Informative = 4,
    DxcCompletionChunk_CurrentParameter = 5,
    DxcCompletionChunk_LeftParen = 6,
    DxcCompletionChunk_RightParen = 7,
    DxcCompletionChunk_LeftBracket = 8,
    DxcCompletionChunk_RightBracket = 9,
    DxcCompletionChunk_LeftBrace = 10,
    DxcCompletionChunk_RightBrace = 11,
    DxcCompletionChunk_LeftAngle = 12,
    DxcCompletionChunk_RightAngle = 13,
    DxcCompletionChunk_Comma = 14,
    DxcCompletionChunk_ResultType = 15,
    DxcCompletionChunk_Colon = 16,
    DxcCompletionChunk_SemiColon = 17,
    DxcCompletionChunk_Equal = 18,
    DxcCompletionChunk_HorizontalSpace = 19,
    DxcCompletionChunk_VerticalSpace = 20,
}
