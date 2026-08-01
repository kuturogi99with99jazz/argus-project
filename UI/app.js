"use strict";

const statusDefinitions = {
  unchecked: { label: "未確認", className: "status-unchecked" },
  initial: { label: "初回取得", className: "status-initial" },
  unchanged: { label: "更新なし", className: "status-unchanged" },
  updated: { label: "更新あり", className: "status-updated" },
  error: { label: "エラー", className: "status-error" }
};

const targets = [
  {
    id: "target-1",
    enabled: true,
    name: "出版社 新刊情報",
    url: "https://example.com/books/new",
    status: "updated",
    checkedAt: "2026/07/31 14:32",
    error: "",
    memo: "新刊ラインナップを確認",
    checking: 0,
    nextStatus: "unchanged"
  },
  {
    id: "target-2",
    enabled: true,
    name: "開発ブログ",
    url: "https://example.com/developer-blog",
    status: "unchanged",
    checkedAt: "2026/07/31 14:31",
    error: "",
    memo: "製品アップデートの記事",
    checking: 0,
    nextStatus: "updated"
  },
  {
    id: "target-3",
    enabled: true,
    name: "公式お知らせ",
    url: "https://example.com/news",
    status: "initial",
    checkedAt: "2026/07/31 14:30",
    error: "",
    memo: "",
    checking: 0,
    nextStatus: "unchanged"
  },
  {
    id: "target-4",
    enabled: true,
    name: "リリースノート",
    url: "https://example.com/releases",
    status: "unchanged",
    checkedAt: "2026/07/31 14:28",
    error: "",
    memo: "チェック中表示の確認用",
    checking: 2,
    nextStatus: "unchanged"
  },
  {
    id: "target-5",
    enabled: true,
    name: "接続確認用ページ",
    url: "https://unavailable.example.com/status",
    status: "error",
    checkedAt: "2026/07/31 14:25",
    error: "ページを取得できませんでした（HTTP 503）",
    memo: "エラー表示の確認用",
    checking: 0,
    nextStatus: "initial"
  },
  {
    id: "target-6",
    enabled: false,
    name: "旧ブログ（停止中）",
    url: "https://example.com/old-blog",
    status: "unchecked",
    checkedAt: "—",
    error: "",
    memo: "移行完了まで無効",
    checking: 0,
    nextStatus: "initial"
  }
];

const tableBody = document.querySelector("#target-table-body");
const targetCount = document.querySelector("#target-count");
const checkingCount = document.querySelector("#checking-count");
const selectionSummary = document.querySelector("#selection-summary");
const statusMessage = document.querySelector("#status-message");
const operationMessage = document.querySelector("#operation-message");
const operationMessageText = document.querySelector("#operation-message-text");

const checkAllButton = document.querySelector("#check-all-button");
const checkSelectedButton = document.querySelector("#check-selected-button");
const openButton = document.querySelector("#open-button");
const addButton = document.querySelector("#add-button");
const editButton = document.querySelector("#edit-button");
const deleteButton = document.querySelector("#delete-button");

const editDialog = document.querySelector("#target-edit-dialog");
const editForm = document.querySelector("#target-edit-form");
const editDialogTitle = document.querySelector("#target-edit-title");
const nameInput = document.querySelector("#target-name");
const urlInput = document.querySelector("#target-url");
const enabledInput = document.querySelector("#target-enabled");
const memoInput = document.querySelector("#target-memo");
const nameError = document.querySelector("#target-name-error");
const urlError = document.querySelector("#target-url-error");
const formOperationError = document.querySelector("#form-operation-error");

const deleteDialog = document.querySelector("#delete-dialog");
const deleteTargetName = document.querySelector("#delete-target-name");

const selectedIds = new Set(["target-1"]);
let editingTargetId = null;
let deleteTargetId = null;
let targetSequence = targets.length;

function getTarget(id) {
  return targets.find((target) => target.id === id);
}

function getSelectedTargets() {
  return targets.filter((target) => selectedIds.has(target.id));
}

function createCell(text, className = "") {
  const cell = document.createElement("td");
  cell.textContent = text;
  if (className) {
    cell.className = className;
  }
  cell.title = text;
  return cell;
}

function createStatusCell(target) {
  const cell = document.createElement("td");
  cell.className = "status-cell";

  const content = document.createElement("div");
  content.className = "status-content";

  const definition = statusDefinitions[target.status];
  const finalStatus = document.createElement("span");
  finalStatus.className = `status-badge ${definition.className}`;
  finalStatus.textContent = definition.label;
  content.append(finalStatus);

  if (target.checking > 0) {
    const checkingStatus = document.createElement("span");
    checkingStatus.className = "status-badge status-checking";
    checkingStatus.textContent = `チェック中 ×${target.checking}`;
    content.append(checkingStatus);
  }

  cell.append(content);
  cell.title = target.checking > 0
    ? `${definition.label} / チェック中 ${target.checking}件`
    : definition.label;
  return cell;
}

function renderTable() {
  tableBody.replaceChildren();

  targets.forEach((target) => {
    const row = document.createElement("tr");
    row.dataset.targetId = target.id;
    row.tabIndex = 0;
    row.setAttribute("aria-selected", String(selectedIds.has(target.id)));
    row.setAttribute(
      "aria-label",
      `${target.name}、${target.enabled ? "有効" : "無効"}、${statusDefinitions[target.status].label}`
    );

    if (!target.enabled) {
      row.classList.add("is-disabled");
    }

    row.append(
      createCell(target.enabled ? "はい" : "いいえ", "enabled-cell"),
      createCell(target.name, "name-cell"),
      createCell(target.url, "url-cell"),
      createStatusCell(target),
      createCell(target.checkedAt, "date-cell"),
      createCell(target.error || "—", target.error ? "error-cell error-text" : "error-cell")
    );

    row.addEventListener("click", (event) => {
      selectTarget(target.id, event.ctrlKey || event.metaKey);
    });

    row.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        selectTarget(target.id, event.ctrlKey || event.metaKey);
      }
    });

    row.addEventListener("dblclick", () => {
      if (target.checking === 0) {
        selectedIds.clear();
        selectedIds.add(target.id);
        render();
        openEditDialog(target);
      }
    });

    tableBody.append(row);
  });
}

function renderSummary() {
  const activeCheckingTargets = targets.filter((target) => target.checking > 0).length;
  targetCount.textContent = `監視対象 ${targets.length}件`;
  checkingCount.textContent = `チェック中 ${activeCheckingTargets}件`;
  selectionSummary.textContent = `選択 ${selectedIds.size}件`;
}

function updateToolbar() {
  const selectedTargets = getSelectedTargets();
  const selectedEnabledTargets = selectedTargets.filter((target) => target.enabled);
  const singleTarget = selectedTargets.length === 1 ? selectedTargets[0] : null;

  checkAllButton.disabled = !targets.some((target) => target.enabled);
  checkSelectedButton.disabled = selectedEnabledTargets.length === 0;
  openButton.disabled = singleTarget === null;
  editButton.disabled = singleTarget === null || singleTarget.checking > 0;
  deleteButton.disabled = singleTarget === null || singleTarget.checking > 0;

  if (singleTarget?.checking > 0) {
    editButton.title = "チェック中の監視対象は編集できません。";
    deleteButton.title = "チェック中の監視対象は削除できません。";
  } else {
    editButton.removeAttribute("title");
    deleteButton.removeAttribute("title");
  }
}

function render() {
  for (const selectedId of selectedIds) {
    if (!getTarget(selectedId)) {
      selectedIds.delete(selectedId);
    }
  }

  renderTable();
  renderSummary();
  updateToolbar();
}

function selectTarget(id, toggle) {
  if (toggle) {
    if (selectedIds.has(id)) {
      selectedIds.delete(id);
    } else {
      selectedIds.add(id);
    }
  } else {
    selectedIds.clear();
    selectedIds.add(id);
  }

  hideOperationMessage();
  render();

  const target = getTarget(id);
  if (target?.status === "error") {
    setStatus(`エラー: ${target.error}`);
  } else {
    setStatus(`${selectedIds.size}件を選択しています。`);
  }
}

function setStatus(message) {
  statusMessage.textContent = message;
}

function showOperationError(message) {
  operationMessageText.textContent = message;
  operationMessage.hidden = false;
}

function hideOperationMessage() {
  operationMessage.hidden = true;
}

function formatCurrentDateTime() {
  const now = new Date();
  const date = new Intl.DateTimeFormat("ja-JP", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false
  }).format(now);
  return date.replace(/\//g, "/");
}

function startChecks(targetIds) {
  const checkTargets = targetIds
    .map((id) => getTarget(id))
    .filter((target) => target?.enabled);

  if (checkTargets.length === 0) {
    showOperationError("チェックできる有効な監視対象が選択されていません。");
    setStatus("チェックを開始できませんでした。");
    return;
  }

  hideOperationMessage();

  checkTargets.forEach((target, index) => {
    target.checking += 1;

    window.setTimeout(() => {
      const currentTarget = getTarget(target.id);
      if (!currentTarget) {
        return;
      }

      currentTarget.checking = Math.max(0, currentTarget.checking - 1);
      currentTarget.status = currentTarget.nextStatus;
      currentTarget.error = "";
      currentTarget.checkedAt = formatCurrentDateTime();
      currentTarget.nextStatus = currentTarget.status === "updated" ? "unchanged" : "updated";
      render();
      setStatus(`${currentTarget.name} のチェックが完了しました。`);
    }, 1100 + (index * 240));
  });

  render();
  setStatus(`${checkTargets.length}件のチェックを開始しました。チェック中も他の操作を行えます。`);
}

function resetFormValidation() {
  nameError.textContent = "";
  urlError.textContent = "";
  nameInput.removeAttribute("aria-invalid");
  urlInput.removeAttribute("aria-invalid");
  formOperationError.hidden = true;
  formOperationError.textContent = "";
}

function openEditDialog(target = null) {
  editingTargetId = target?.id ?? null;
  editDialogTitle.textContent = target ? "監視対象を編集" : "監視対象を追加";
  nameInput.value = target?.name ?? "";
  urlInput.value = target?.url ?? "";
  enabledInput.checked = target?.enabled ?? true;
  memoInput.value = target?.memo ?? "";
  resetFormValidation();
  editDialog.showModal();
  window.setTimeout(() => nameInput.focus(), 0);
}

function closeEditDialog() {
  editDialog.close();
  editingTargetId = null;
  addButton.focus();
}

function validateForm() {
  resetFormValidation();
  let isValid = true;

  if (nameInput.value.trim().length === 0) {
    nameError.textContent = "名前を入力してください。";
    nameInput.setAttribute("aria-invalid", "true");
    isValid = false;
  }

  try {
    const parsedUrl = new URL(urlInput.value.trim());
    if (parsedUrl.protocol !== "http:" && parsedUrl.protocol !== "https:") {
      throw new Error("Unsupported protocol");
    }
  } catch {
    urlError.textContent = "http:// または https:// で始まる正しいURLを入力してください。";
    urlInput.setAttribute("aria-invalid", "true");
    isValid = false;
  }

  if (!isValid) {
    const firstInvalid = editForm.querySelector('[aria-invalid="true"]');
    firstInvalid?.focus();
  }

  return isValid;
}

function saveTarget() {
  if (!validateForm()) {
    return;
  }

  const name = nameInput.value.trim();
  const url = urlInput.value.trim();

  if (editingTargetId) {
    const target = getTarget(editingTargetId);
    if (!target) {
      formOperationError.textContent = "対象が見つからないため保存できませんでした。一覧を確認してください。";
      formOperationError.hidden = false;
      return;
    }

    const urlChanged = target.url !== url;
    target.name = name;
    target.url = url;
    target.enabled = enabledInput.checked;
    target.memo = memoInput.value.trim();

    if (urlChanged) {
      target.status = "unchecked";
      target.checkedAt = "—";
      target.error = "";
      target.nextStatus = "initial";
    }

    setStatus(`${target.name} を更新しました。`);
  } else {
    targetSequence += 1;
    const target = {
      id: `target-${targetSequence}`,
      enabled: enabledInput.checked,
      name,
      url,
      status: "unchecked",
      checkedAt: "—",
      error: "",
      memo: memoInput.value.trim(),
      checking: 0,
      nextStatus: "initial"
    };
    targets.push(target);
    selectedIds.clear();
    selectedIds.add(target.id);
    setStatus(`${target.name} を追加しました。`);
  }

  editDialog.close();
  editingTargetId = null;
  render();
}

function openDeleteDialog(target) {
  deleteTargetId = target.id;
  deleteTargetName.textContent = `「${target.name}」`;
  deleteDialog.showModal();
  window.setTimeout(() => document.querySelector("#cancel-delete-button").focus(), 0);
}

function closeDeleteDialog() {
  deleteDialog.close();
  deleteTargetId = null;
  deleteButton.focus();
}

checkAllButton.addEventListener("click", () => {
  startChecks(targets.filter((target) => target.enabled).map((target) => target.id));
});

checkSelectedButton.addEventListener("click", () => {
  startChecks(getSelectedTargets().filter((target) => target.enabled).map((target) => target.id));
});

openButton.addEventListener("click", () => {
  const [target] = getSelectedTargets();
  if (!target) {
    return;
  }

  if (target.status === "error") {
    showOperationError("ブラウザを開けませんでした。URLを確認してからもう一度お試しください。");
    setStatus("ブラウザ起動エラーを表示しています。");
    return;
  }

  hideOperationMessage();
  setStatus(`モック表示: ${target.name} を既定ブラウザで開きます。`);
});

addButton.addEventListener("click", () => openEditDialog());

editButton.addEventListener("click", () => {
  const [target] = getSelectedTargets();
  if (target && target.checking === 0) {
    openEditDialog(target);
  }
});

deleteButton.addEventListener("click", () => {
  const [target] = getSelectedTargets();
  if (target && target.checking === 0) {
    openDeleteDialog(target);
  }
});

document.querySelector("#dismiss-message-button").addEventListener("click", () => {
  hideOperationMessage();
  openButton.focus();
});

document.querySelectorAll("[data-dialog-close]").forEach((button) => {
  button.addEventListener("click", closeEditDialog);
});

document.querySelector("#cancel-edit-button").addEventListener("click", closeEditDialog);

editForm.addEventListener("submit", (event) => {
  event.preventDefault();
  saveTarget();
});

document.querySelectorAll("[data-delete-close]").forEach((button) => {
  button.addEventListener("click", closeDeleteDialog);
});

document.querySelector("#cancel-delete-button").addEventListener("click", closeDeleteDialog);

document.querySelector("#confirm-delete-button").addEventListener("click", () => {
  const target = deleteTargetId ? getTarget(deleteTargetId) : null;
  if (!target) {
    closeDeleteDialog();
    return;
  }

  const targetIndex = targets.findIndex((item) => item.id === target.id);
  targets.splice(targetIndex, 1);
  selectedIds.delete(target.id);
  deleteDialog.close();
  deleteTargetId = null;
  render();
  setStatus(`${target.name} を削除しました。`);
  addButton.focus();
});

editDialog.addEventListener("cancel", () => {
  editingTargetId = null;
  addButton.focus();
});

deleteDialog.addEventListener("cancel", () => {
  deleteTargetId = null;
  deleteButton.focus();
});

render();
