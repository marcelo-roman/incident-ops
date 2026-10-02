(function () {
  try {
    var preference = window.localStorage.getItem('incident-ops.theme');
    if (preference === 'light' || preference === 'dark') {
      document.documentElement.setAttribute('data-theme', preference);
    }
  } catch (error) {
    return;
  }
})();
