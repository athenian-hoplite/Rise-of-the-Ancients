namespace ROTA.Memory
{

/// <summary>
/// Interface that expresses the behaviour of an object that knows how to reset its own memory. 
/// </summary>
public interface IResetable
{
    
    /// <summary>
    /// Reset own state to default (empty ?) state.
    /// </summary>
    void Reset();

}

}