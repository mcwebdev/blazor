const connections = new Map();

function getValue(value, camelName, pascalName) {
  return value?.[camelName] ?? value?.[pascalName];
}

async function notify(dotNetRef, methodName, ...args) {
  try {
    await dotNetRef.invokeMethodAsync(methodName, ...args);
  } catch (error) {
    console.warn(`FlowBoard realtime callback failed: ${methodName}`, error);
  }
}

function requireSignalR() {
  if (!window.signalR) {
    throw new Error("The SignalR JavaScript client was not loaded.");
  }

  return window.signalR;
}

export async function connect(boardId, dotNetRef) {
  const signalR = requireSignalR();
  const key = String(boardId);

  await disconnect(key);
  await notify(dotNetRef, "HandleConnectionStateChanged", "Connecting");

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/board")
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  connection.on("BoardActivityRecorded", (evt) => {
    const eventBoardId = String(getValue(evt, "boardId", "BoardId"));
    const summary = getValue(evt, "summary", "Summary") ?? "";
    return notify(dotNetRef, "HandleBoardActivityRecorded", eventBoardId, summary);
  });

  connection.on("UserJoinedBoard", (users) =>
    notify(dotNetRef, "HandlePresenceChanged", users ?? []));

  connection.on("UserLeftBoard", (users) =>
    notify(dotNetRef, "HandlePresenceChanged", users ?? []));

  connection.on("UserStartedEditing", (userName, taskId, fieldName) =>
    notify(dotNetRef, "HandleUserStartedEditing", userName, String(taskId), fieldName));

  connection.on("UserStoppedEditing", (userName, taskId, fieldName) =>
    notify(dotNetRef, "HandleUserStoppedEditing", userName, String(taskId), fieldName));

  connection.onreconnecting(() =>
    notify(dotNetRef, "HandleConnectionStateChanged", "Reconnecting"));

  connection.onreconnected(async () => {
    await notify(dotNetRef, "HandleConnectionStateChanged", "Connected");
    await connection.invoke("JoinBoard", key);
  });

  connection.onclose(() => {
    connections.delete(key);
    return notify(dotNetRef, "HandleConnectionStateChanged", "Disconnected");
  });

  connections.set(key, connection);

  try {
    await connection.start();
    await notify(dotNetRef, "HandleConnectionStateChanged", "Connected");
    await connection.invoke("JoinBoard", key);
  } catch (error) {
    connections.delete(key);
    await notify(dotNetRef, "HandleConnectionStateChanged", "Disconnected");
    throw error;
  }
}

export async function disconnect(boardId) {
  const signalR = requireSignalR();
  const key = String(boardId);
  const connection = connections.get(key);

  if (!connection) {
    return;
  }

  connections.delete(key);

  if (connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke("LeaveBoard", key);
  }

  await connection.stop();
}

export async function startEditing(boardId, taskId, fieldName) {
  await invokeIfConnected(boardId, "StartEditing", String(taskId), fieldName);
}

export async function stopEditing(boardId, taskId, fieldName) {
  await invokeIfConnected(boardId, "StopEditing", String(taskId), fieldName);
}

async function invokeIfConnected(boardId, methodName, taskId, fieldName) {
  const signalR = requireSignalR();
  const key = String(boardId);
  const connection = connections.get(key);

  if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
    return;
  }

  await connection.invoke(methodName, key, taskId, fieldName);
}
