namespace X2CharacterPool.Serialization;

public class X2SerializationException : Exception
{
    public X2SerializationException()
    {
    }

    public X2SerializationException(string message)
        : base(message)
    {
    }

    public X2SerializationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}