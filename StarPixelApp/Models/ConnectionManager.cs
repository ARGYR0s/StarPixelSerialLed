using System;
using System.Threading.Tasks;
using StarPixelApp.Connections;

public class ConnectionManager
{
    private IDeviceConnection _connection;
    private readonly string _deviceId;
    private CancellationTokenSource _cancellationTokenSource = new();
    private volatile bool _isConnected = false;
    private int _reconnectAttempts = 0; // Moved to class scope
    private const int PING_INTERVAL_MS = 5000;
    private const int PING_TIMEOUT_MS = 5000;
    private const int MAX_RECONNECT_ATTEMPTS = 3;

    public event EventHandler<bool> ConnectionStatusChanged;

    public bool IsConnected => _isConnected;

    public ConnectionManager(string connectionType, string deviceId)
    {
        _connection = ConnectionFactory.CreateConnection(connectionType);
        _deviceId = deviceId;

        _connection.DataReceived += OnDataReceived;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task<bool> ConnectAsync()
    {
        //return await _connection.ConnectAsync(_deviceId);

        try
        {
            _isConnected = await _connection.ConnectAsync(_deviceId);
            if (_isConnected)
            {
                ConnectionStatusChanged?.Invoke(this, true);
                _cancellationTokenSource = new CancellationTokenSource();
                _ = MaintainConnectionAsync(_cancellationTokenSource.Token);
            }
            return _isConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
            return false;
        }
    }
    /*
    public void Disconnect()
    {
        _connection.DisconnectAsync();
    }

    public async Task SendDataAsync(byte[] data)
    {
        await _connection.SendDataAsync(data);
    }
    
    private void OnDataReceived(byte[] data)
    {
        EventBus.Publish("deviceDataReceived", data);
    }
    */
    public async Task StartAsync()
    {
        /*
        _isConnected = await _connection.ConnectAsync(_deviceId);
        if (_isConnected)
        {
            Console.WriteLine("Соединение установлено, запускаем поддержку соединения...");
            _ = MaintainConnectionAsync(_cancellationTokenSource.Token);
        }
        */
        if (!_isConnected)
        {
            _isConnected = await ConnectAsync();
        }

        if (_isConnected)
        {
            Console.WriteLine("Connection established, starting connection maintenance...");
            _ = MaintainConnectionAsync(_cancellationTokenSource.Token);
        }
        else
        {
            Console.WriteLine("Initial connection failed");
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        if (_connection != null)
        {
            _connection.DisconnectAsync().Wait();
        }

        //_connection.DisconnectAsync();
        //_isConnected = false;
        UpdateConnectionStatus(false);
        _reconnectAttempts = 0;
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        if (_isConnected != isConnected)
        {
            _isConnected = isConnected;
            ConnectionStatusChanged?.Invoke(this, isConnected);
        }
    }

    private async Task MaintainConnectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_isConnected)
                {
                    /*
                    Console.WriteLine("Sending ping...");
                    byte[] pingData = { 0x00, 0x00 };
                    await _connection.SendDataAsync(pingData);

                    bool pongReceived = await WaitForPongAsync(
                        TimeSpan.FromMilliseconds(PING_TIMEOUT_MS),
                        cancellationToken);

                    if (pongReceived)
                    {
                        Console.WriteLine("Pong received, connection active");
                        _reconnectAttempts = 0; // Reset on successful ping
                    }
                    else
                    {
                        Console.WriteLine("Pong not received, connection lost");
                        await HandleReconnectionAsync();
                    }
                    */
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Maintenance error: {ex.Message}");
                await HandleReconnectionAsync();
            }

            await Task.Delay(PING_INTERVAL_MS, cancellationToken);
        }
    }

    private async Task HandleReconnectionAsync()
    {
        UpdateConnectionStatus(false);

        if (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
        {
            _reconnectAttempts++;
            Console.WriteLine($"Attempting reconnection ({_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})...");

            _isConnected = await ConnectAsync();
            if (_isConnected)
            {
                Console.WriteLine("Reconnection successful");
                _reconnectAttempts = 0;
            }
        }
        else
        {
            Console.WriteLine("Max reconnection attempts reached. Connection failed.");
            Stop();
        }
    }

    private async Task<bool> WaitForPongAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        void PongReceived(byte[] data)
        {
            if (data.Length > 0 && data[0] == 0x00)
            {
                tcs.TrySetResult(true);
            }
        }

        try
        {
            _connection.DataReceived += PongReceived;

            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            return completedTask == tcs.Task && tcs.Task.Result;
        }
        finally
        {
            _connection.DataReceived -= PongReceived;
        }
    }

    private void OnDataReceived(byte[] data)
    {
        //byte[] data1 = { 0x00, 0x01, 0x00 };
        //SendDataAsync(data);
        //Console.WriteLine($"Получены данные: {data}");
        AsyncEventBus.Publish("deviceDataReceived", data);
    }

    public async Task SendDataAsync(byte[] data)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Cannot send data: not connected");
        }

        await _connection.SendDataAsync(data);
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
        _connection.DataReceived -= OnDataReceived;
        _connection = null;
    }
}
