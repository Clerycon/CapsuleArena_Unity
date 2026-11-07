public class PredictionHistoryBuffer
{
    private readonly PredictionRecord[] _records;

    public int Capacity => _records.Length;

    public PredictionHistoryBuffer(int capacity)
    {
        if(capacity < 1)
        {
            LogManager.LogWarning(this, $"Invalid buffer capacity {capacity}, using 1.");
            capacity = 1;
        }

        _records = new PredictionRecord[capacity];
    }
    
    public void Add(PredictionRecord record)
    {
        _records[GetIndex(record.Sequence)] = record;
    }

    public bool TryGet(uint sequence, out PredictionRecord record)
    {
        if(sequence == 0)
        {
            record = default;
            return false;
        }

        PredictionRecord storedRecord = _records[GetIndex(sequence)];

        if(storedRecord.Sequence == sequence)
        {
            record = storedRecord;
            return true;
        }

        record = default;
        return false;
    }

    public void Clear()
    {
        System.Array.Clear(_records, 0, _records.Length);
    }

    private int GetIndex(uint sequence) => (int)(sequence % (uint)_records.Length);
}
