public interface IBox
{
    int ColorId { get; }
    int CurrentLoad { get; }
    int Capacity { get; }
    bool IsFull { get; }

    void StartMove();
}

