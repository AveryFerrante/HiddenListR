// Events page: manage event members dialog.

let _manageEventId = 0;

async function prepareManageUsers(eventId, title) {
  _manageEventId = eventId;
  document.getElementById('manageUsersTitle').textContent = 'Manage Users - ' + title;
  document.getElementById('manageUsersMsg').textContent = '';
  await reloadManageUsers();
}

document.addEventListener('DOMContentLoaded', () => {
  const modal = document.getElementById('manageUsersModal');
  if (modal) modal.addEventListener('close', () => location.reload());
});

function antiForgeryToken() {
  const el = document.querySelector('input[name="__RequestVerificationToken"]');
  return el ? el.value : '';
}

async function reloadManageUsers() {
  const res = await fetch(`?handler=Members&eventId=${_manageEventId}`);
  if (!res.ok) { document.getElementById('manageUsersMsg').textContent = 'Failed to load members.'; return; }
  const data = await res.json();

  const addSel = document.getElementById('addUserSelect');
  addSel.innerHTML = '';
  if (data.availableUsers.length === 0) {
    const opt = document.createElement('option'); opt.textContent = 'No users available'; opt.value = ''; addSel.appendChild(opt);
  } else {
    for (const u of data.availableUsers) {
      const opt = document.createElement('option');
      opt.value = u.id; opt.textContent = `${u.name} (${u.phone})`; addSel.appendChild(opt);
    }
  }

  const list = document.getElementById('membersList');
  list.innerHTML = '';
  if (data.members.length === 0) {
    list.textContent = 'No members.';
  } else {
    const ul = document.createElement('ul');
    ul.className = 'unstyled vstack gap-4';
    for (const m of data.members) {
      const li = document.createElement('li');
      const label = document.createElement('label');
      const cb = document.createElement('input');
      cb.type = 'checkbox'; cb.value = m.id; cb.dataset.count = m.wishListItemCount; cb.dataset.name = m.name;
      const text = document.createElement('span');
      text.textContent = `${m.name} (${m.phone}) - ${m.wishListItemCount} wish list item(s)`;
      label.appendChild(cb); label.appendChild(text);
      li.appendChild(label);
      ul.appendChild(li);
    }
    list.appendChild(ul);
  }
}

async function addUserToEvent() {
  const userId = parseInt(document.getElementById('addUserSelect').value, 10);
  if (!userId) return;
  const res = await fetch('?handler=AddMember', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken() },
    body: JSON.stringify({ eventId: _manageEventId, userId })
  });
  if (!res.ok) { document.getElementById('manageUsersMsg').textContent = 'Failed to add user.'; return; }
  await reloadManageUsers();
}

async function removeSelectedUsers() {
  const checked = Array.from(document.querySelectorAll('#membersList input[type="checkbox"]:checked'));
  if (checked.length === 0) return;
  const withItems = checked.filter(c => parseInt(c.dataset.count, 10) > 0);
  let msg = `Remove ${checked.length} user(s) from this event?`;
  if (withItems.length > 0) {
    const details = withItems.map(c => `${c.dataset.name} (${c.dataset.count} item(s))`).join(', ');
    msg = `Warning: ${details} have wish list items for this event. Removing them will cascade delete their wish list items for this event. Proceed?`;
  }
  if (!confirm(msg)) return;

  const userIds = checked.map(c => parseInt(c.value, 10));
  const res = await fetch('?handler=RemoveMembers', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiForgeryToken() },
    body: JSON.stringify({ eventId: _manageEventId, userIds })
  });
  if (!res.ok) { document.getElementById('manageUsersMsg').textContent = 'Failed to remove users.'; return; }
  await reloadManageUsers();
}
