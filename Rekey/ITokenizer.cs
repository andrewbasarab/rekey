namespace Rekey;

public interface ITokenizer
{
    TokenizerResponse Tokenize(string input);
}
