namespace ROTA.Utils
{

/// <summary>
/// Struct that wraps a value T and expresses the semantic possibility of it not being present.
/// </summary>
public struct Optional<T>
{

    /// <summary>
    /// Indicates wheter there is an underlying value present (true) or if the Optional is empty (false), meaning the
    /// value is the default value for the type (e.g. null for reference types).
    /// </summary>
    public bool IsPresent { get { return m_isPresent; } }

    /// <summary>
    /// Get the underlying value. If the Optional is empty (IsPresent = false) acessing this property will throw
    /// System.MissingMemberException.
    /// </summary>
    public T Value 
    {
        get 
        {
            if (m_isPresent) return m_value;
            else throw new System.MissingMemberException("Tried to acess the value of an Optional that has no value present.");
        }
    }

    private T m_value; // Default value is default(T)
    private bool m_isPresent; // Default value is false

    /// <summary>
    /// Creates a valid optional from the given value.
    /// To create an invalid (empty) optional use the default empty constructor.
    /// </summary>
    public Optional(T value)
    {
        m_value = value;
        m_isPresent = value != null; // Sanity check for "new Optional<T>(null)".
    }

    /// <summary>
    /// Implicit conversion from T to Optional<T>.
    /// 
    /// T someVal = new T();
    /// Optional<T> op = someVal;
    /// </summary>
    public static implicit operator Optional<T>(T value)
    {
        if (value == null)
        {
            return new Optional<T>();
        }
        else
        {
            return new Optional<T>(value);
        }
    }

    /// <summary>
    /// Implicit conversion from Optional<T> to bool.
    /// 
    /// Optional<T> op = new Optional<T>();
    /// if (op) // Will fail, op.IsPresent = false
    /// </summary>
    public static implicit operator bool(Optional<T> optional)
    {
        return optional.m_isPresent;
    }

    /// <summary>
    /// Explicit cast from Optional<T> to T.
    /// 
    /// Optional<T> optional = new Optional<T>();
    /// T val = (T) optional;
    /// 
    /// Note: If casted optional is empty will throw System.MissingMemberException.
    /// As such this explicit cast should only be used when there is certainty, due to
    /// implementation details, that the optional is not empty and that, as a consequence,
    /// there is no need to check IsPresent.
    /// </summary>
    public static explicit operator T(Optional<T> optional)
    {
        return optional.Value;
    }

}

}