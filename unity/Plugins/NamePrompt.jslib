// Opens the phone's own "type your name" box, so the keyboard pops up.
mergeInto(LibraryManager.library, {
  AskName: function (current) {
    var cur = UTF8ToString(current);
    var n = window.prompt("What's your name?", cur);
    if (n === null) n = cur;
    var size = lengthBytesUTF8(n) + 1;
    var buf = _malloc(size);
    stringToUTF8(n, buf, size);
    return buf;
  }
});
