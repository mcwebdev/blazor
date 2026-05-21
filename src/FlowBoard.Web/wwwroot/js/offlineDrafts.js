// FlowBoard offline drafts + pending action queue.
//
// Two responsibilities, one module because they share the same localStorage
// primitives and the same "is the browser actually online?" signal:
//
//   1. Task draft autosave/restore. Each board has a single in-progress draft
//      keyed by `flowboard:draft:<boardId>`. Saved on every keystroke from the
//      TaskDrawer create form; cleared when the create succeeds.
//
//   2. Pending action queue. When a mutation (create-task, add-comment) is
//      issued while the SignalR connection is not Connected, we enqueue it
//      under `flowboard:queue:<boardId>` along with a generated idempotency
//      key. On reconnect the board page calls drainQueue() and we replay each
//      action through the page's [JSInvokable] handlers. Server-side
//      idempotency guards ensure replays do not produce duplicates.

const DRAFT_PREFIX = "flowboard:draft:";
const QUEUE_PREFIX = "flowboard:queue:";

function storageAvailable() {
  try {
    if (typeof window === "undefined" || !window.localStorage) return false;
    const probe = "__flowboard_probe__";
    window.localStorage.setItem(probe, "1");
    window.localStorage.removeItem(probe);
    return true;
  } catch {
    return false;
  }
}

function draftKey(boardId) {
  return `${DRAFT_PREFIX}${String(boardId)}`;
}

function queueKey(boardId) {
  return `${QUEUE_PREFIX}${String(boardId)}`;
}

// Lightweight uuid; we don't need crypto-strength uniqueness, only collision
// resistance across a single user's outstanding queue. Falls back gracefully
// where crypto.randomUUID is unavailable (older Safari, file://).
function newRequestId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return "req-" + Math.random().toString(36).slice(2) + "-" + Date.now().toString(36);
}

// ── Drafts ─────────────────────────────────────────────────────────────────

export function saveDraft(boardId, draft) {
  if (!storageAvailable()) return;

  try {
    const payload = { ...draft, savedAtUtc: new Date().toISOString() };
    window.localStorage.setItem(draftKey(boardId), JSON.stringify(payload));
  } catch (error) {
    console.warn("FlowBoard draft save failed", error);
  }
}

export function loadDraft(boardId) {
  if (!storageAvailable()) return null;

  try {
    const raw = window.localStorage.getItem(draftKey(boardId));
    if (!raw) return null;
    return JSON.parse(raw);
  } catch (error) {
    console.warn("FlowBoard draft load failed", error);
    return null;
  }
}

export function clearDraft(boardId) {
  if (!storageAvailable()) return;

  try {
    window.localStorage.removeItem(draftKey(boardId));
  } catch {
    // ignore: clearing failure is non-fatal
  }
}

// ── Queue ──────────────────────────────────────────────────────────────────

function readQueue(boardId) {
  if (!storageAvailable()) return [];

  try {
    const raw = window.localStorage.getItem(queueKey(boardId));
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function writeQueue(boardId, items) {
  if (!storageAvailable()) return;

  try {
    if (items.length === 0) {
      window.localStorage.removeItem(queueKey(boardId));
    } else {
      window.localStorage.setItem(queueKey(boardId), JSON.stringify(items));
    }
  } catch (error) {
    console.warn("FlowBoard queue write failed", error);
  }
}

export function enqueueAction(boardId, kind, payload) {
  const entry = {
    id: newRequestId(),
    kind,
    payload,
    enqueuedAtUtc: new Date().toISOString()
  };

  const items = readQueue(boardId);
  items.push(entry);
  writeQueue(boardId, items);

  return entry;
}

export function pendingCount(boardId) {
  return readQueue(boardId).length;
}

export function listPending(boardId) {
  return readQueue(boardId);
}

// Drain the queue by handing each entry back to the board page through its
// [JSInvokable] handler. The Blazor side calls IBoardService for us and
// reports back true/false per item. We keep failed items in the queue with an
// incremented attempt counter and an error reason so the operator can see
// what bounced.
export async function drainQueue(boardId, dotNetRef, methodName) {
  const items = readQueue(boardId);
  if (items.length === 0) return { attempted: 0, succeeded: 0, failed: 0 };

  let succeeded = 0;
  let failed = 0;
  const remaining = [];

  for (const item of items) {
    try {
      const accepted = await dotNetRef.invokeMethodAsync(
        methodName,
        item.kind,
        item.id,
        item.payload
      );

      if (accepted === true) {
        succeeded += 1;
      } else {
        failed += 1;
        remaining.push({
          ...item,
          attempts: (item.attempts ?? 0) + 1,
          lastError: "Server rejected the action.",
          lastTriedAtUtc: new Date().toISOString()
        });
      }
    } catch (error) {
      failed += 1;
      remaining.push({
        ...item,
        attempts: (item.attempts ?? 0) + 1,
        lastError: error?.message ?? "Unknown error during replay.",
        lastTriedAtUtc: new Date().toISOString()
      });
    }
  }

  writeQueue(boardId, remaining);
  return { attempted: items.length, succeeded, failed };
}

export function discardQueue(boardId) {
  writeQueue(boardId, []);
}
