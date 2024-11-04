const iosAgent = () => {
  return navigator.userAgent.match(/(iPhone|iPod|iPad);?/i);
};

const _execCommand = (action) => {
  let is = document.execCommand(action);
  if (is) {
    console.log("复制成功");
  } else {
    console.log("复制失败");
  }
};

export default function (message: string) {
  if (iosAgent()) {
    console.log("input 复制方式 " + message);
    let inputObj = document.createElement("input");
    inputObj.value = message;
    document.body.appendChild(inputObj);
    inputObj.select();
    inputObj.setSelectionRange(0, inputObj.value.length);
    _execCommand("Copy");
    document.body.removeChild(inputObj);
  } else {
    console.log("document 复制方式 " + message);
    let domObj = document.createElement("span");
    domObj.innerHTML = message;
    document.body.appendChild(domObj);
    let selection = window.getSelection();
    let range = document.createRange();
    range.selectNodeContents(domObj);
    selection.removeAllRanges();
    selection.addRange(range);
    _execCommand("Copy");
    document.body.removeChild(domObj);
  }
}
