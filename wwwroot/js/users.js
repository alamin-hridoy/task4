(() => {
  const selectAll = document.getElementById("selectAll");
  const rowSelectors = Array.from(document.querySelectorAll(".row-selector"));
  const actionButtons = Array.from(document.querySelectorAll(".bulk-action-btn"));

  if (!selectAll || rowSelectors.length === 0) {
    return;
  }

  const syncToolbarState = () => {
    const selectedCount = rowSelectors.filter((checkbox) => checkbox.checked).length;
    actionButtons.forEach((button) => {
      button.disabled = selectedCount === 0;
    });

    selectAll.checked = selectedCount > 0 && selectedCount === rowSelectors.length;
    selectAll.indeterminate = selectedCount > 0 && selectedCount < rowSelectors.length;
  };

  selectAll.addEventListener("change", () => {
    rowSelectors.forEach((checkbox) => {
      checkbox.checked = selectAll.checked;
    });
    syncToolbarState();
  });

  rowSelectors.forEach((checkbox) => {
    checkbox.addEventListener("change", syncToolbarState);
  });

  syncToolbarState();
})();
