using UnityEngine;

public interface IGameModule
{
    public abstract void Load();
    public abstract void Initialize();

    public bool IsLoaded { get; } 
}
