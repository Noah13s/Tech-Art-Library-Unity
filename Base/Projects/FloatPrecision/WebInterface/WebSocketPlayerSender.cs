using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketPlayerSender : MonoBehaviour
{
    [SerializeField] private FloatPrecisionPlayer player;
    [SerializeField, Min(0.1f)] private float connectionRetryDelay = 0.25f;
    [SerializeField, Min(1)] private int connectionAttempts = 20;

    private ClientWebSocket webSocket;
    private readonly Uri serverUri = new("ws://localhost:3000");
    private CancellationTokenSource cts;
    private bool sendInProgress;

    async void Start()
    {
        cts = new CancellationTokenSource();
        await ConnectWithRetryAsync(cts.Token);
    }

    async void Update()
    {
        if (sendInProgress || webSocket == null || webSocket.State != WebSocketState.Open || player == null)
        {
            return;
        }

        sendInProgress = true;

        try
        {
            DoubleVector3 pos = player.playerPosition;
            // Structure message as JSON
            string msg = FormattableString.Invariant($"{{\"type\":\"playerPosition\",\"x\":{pos.x},\"y\":{pos.y},\"z\":{pos.z}}}");
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when exiting Play mode.
        }
        catch (WebSocketException exception)
        {
            Debug.LogWarning($"WebSocket send stopped: {exception.Message}", this);
        }
        finally
        {
            sendInProgress = false;
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        WebSocketException lastException = null;

        for (int attempt = 1; attempt <= connectionAttempts && !cancellationToken.IsCancellationRequested; attempt++)
        {
            webSocket?.Dispose();
            webSocket = new ClientWebSocket();

            try
            {
                await webSocket.ConnectAsync(serverUri, cancellationToken);
                Debug.Log("Connected to WebSocket server.", this);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (WebSocketException exception)
            {
                lastException = exception;
            }

            if (attempt < connectionAttempts)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(connectionRetryDelay), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            Debug.LogWarning(
                $"Could not connect to WebSocket server at {serverUri} after {connectionAttempts} attempts. " +
                $"Make sure the Node server started successfully. {lastException?.Message}",
                this);
        }
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        webSocket?.Abort();
        webSocket?.Dispose();
        webSocket = null;
        cts?.Dispose();
        cts = null;
    }
}
