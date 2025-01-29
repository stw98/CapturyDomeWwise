using System;

public class CircularBuffer<T>
{
    private T[] buffer;         // The buffer array
    private int head;           // The index of the next write position
    private int tail;           // The index of the next read position
    private int size;           // The current size of the buffer
    private readonly int capacity; // The maximum capacity of the buffer
    private int delaySize; // Buffer delay in milliseconds

    public CircularBuffer(int capacity, int delaySize)
    {
        if (capacity < 1)
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
        
        this.capacity = capacity;
        buffer = new T[capacity];
        head = 0;
        tail = 0;
        size = 0;
        this.delaySize = delaySize;
    }

    // Adds an item to the buffer
    public void Write(T item)
    {
        buffer[head] = item;
        head = (head + 1) % capacity;

        if (size == capacity)
        {
            // Overwrite: move the tail forward
            tail = (tail + 1) % capacity;
        }
        else
        {
            size++;
        }
    }

    // Reads an item from the buffer
    public T Read()
    {
        if (IsEmpty)
            throw new InvalidOperationException("The buffer is empty.");

        T item = buffer[tail];
        tail = (tail + 1) % capacity;
        size--;
        return item;
    }

    // Peek at the next item without removing it
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("The buffer is empty.");

        return buffer[tail];
    }

    // Indicates whether the buffer is empty
    public bool IsEmpty => size == 0;

    // Indicates whether the buffer is full
    public bool IsFull => size == capacity;

    // Gets the current size of the buffer
    public int CurrentSize => size;
    //Indicates whether the buffer is ready
    public bool IsReady => CurrentSize >= delaySize;

    // Clears the buffer
    public void Clear()
    {
        head = 0;
        tail = 0;
        size = 0;
        Array.Clear(buffer, 0, capacity);
    }
}
