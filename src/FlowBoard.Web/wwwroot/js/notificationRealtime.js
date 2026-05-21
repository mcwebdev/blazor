let connection = null;

function requireSignalR() {
  if (!window.signalR) {
    throw new Error("The SignalR JavaScript client was not loaded.");
  }
  return window.signalR;
}

export async function connect(dotNetRef) {
  const signalR = requireSignalR();

  await disconnect();

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  connection.on("NotificationCountChanged", (count) => {
    try {
      dotNetRef.invokeMethodAsync("HandleNotificationCountChanged", count);
    } catch (error) {
      console.warn("FlowBoard notification callback failed:", error);
    }
  });

  try {
    await connection.start();
  } catch (error) {
    console.error("Failed to connect to notification hub", error);
    throw error;
  }
}

export async function disconnect() {
  if (!connection) {
    return;
  }

  await connection.stop();
  connection = null;
}
