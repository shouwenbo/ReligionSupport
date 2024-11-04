const container = document.getElementById('links_container');
let result = '';

if (container) {
    const elements = container.querySelectorAll('[src], [href]');
    const urls = [];

    elements.forEach(el => {
        const rawUrl = el.getAttribute('src') || el.getAttribute('href') || '';
        const url = rawUrl.toLowerCase();
        if (
            url.endsWith('.mp3') ||
            /\.(jpg|jpeg|png|gif|webp)$/.test(url)
        ) {
            try {
                urls.push(encodeURI(rawUrl));
            } catch (e) {
                console.warn('❌ URL 编码失败：', rawUrl);
            }
        }
    });

    result = urls.join('\n');

    // ✅ 控制台打印
    console.log('📋 提取结果如下：\n' + result);

    // ✅ 创建按钮
    const btn = document.createElement('button');
    btn.textContent = '📋 点击复制链接';
    Object.assign(btn.style, {
        position: 'fixed',
        top: '20px',
        right: '20px',
        zIndex: 9999,
        padding: '10px 14px',
        fontSize: '14px',
        background: '#4caf50',
        color: '#fff',
        border: 'none',
        borderRadius: '6px',
        cursor: 'pointer',
    });

    btn.onclick = () => {
        navigator.clipboard.writeText(result).then(() => {
            alert('✅ 链接已复制到剪贴板');
            btn.remove(); // ✅ 移除按钮
        }).catch(err => {
            alert('❌ 复制失败，请检查权限');
            console.error(err);
        });
    };

    document.body.appendChild(btn);
} else {
    console.warn('❌ 未找到元素：#links_container');
}
