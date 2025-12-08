// ==UserScript==
// @name         Chzzk Auto-Complete
// @namespace    http://tampermonkey.net/
// @version      2025. 12.02
// @description  치지직에서 가사를 자동완성 해줍니다.   일치하는 부분이 있는 가사 찾기(초성도 가능), 입력이 빈 칸일 때 다음 가사 미리 띄우기 등의 기능이 있습니다.
// @author       Nata
// @license      MIT
// @match        https://chzzk.naver.com/*
// @match        https://play.sooplive.co.kr/*
// @match        https://vod.sooplive.co.kr/*
// @grant        GM_addStyle
// ==/UserScript==

(function() {
  'use strict';

    // ===== 1. 백업/복원 유틸 =====
  function exportBackup() {
    const templates = JSON.parse(localStorage.getItem('ac.templates') || '[]');
    const settings = JSON.parse(localStorage.getItem('ac.settings') || '{}');

    const backup = {
      version: '2024.12.22',
      exportDate: new Date().toISOString(),
      templates: templates,
      settings: settings
    };

    const dataStr = JSON.stringify(backup, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);

    const now = new Date();
    const filename = `cac-backup-${
      now.getFullYear()
    }${String(now.getMonth() + 1).padStart(2, '0')}${
      String(now.getDate()).padStart(2, '0')
    }-${
      String(now.getHours()).padStart(2, '0')
    }${String(now.getMinutes()).padStart(2, '0')}${
      String(now.getSeconds()).padStart(2, '0')
    }.json`;

    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    console.log(`[CAC] Backup exported: ${filename}`);
    alert(`✅ 백업 파일이 다운로드되었습니다: ${filename}`);
  }

  function importBackup(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = (e) => {
        try {
          const backup = JSON.parse(e.target.result);

          if (! backup.templates || !backup.settings) {
            throw new Error('Invalid backup file format');
          }

          // 새 데이터 저장
          localStorage.setItem('ac.templates', JSON.stringify(backup.templates));
          localStorage. setItem('ac.settings', JSON.stringify(backup.settings));

          console.log('[CAC] Backup restored successfully');
          alert(`✅ 백업이 복원되었습니다!\n템플릿: ${backup.templates. length}개\n설정이 업데이트되었습니다. `);

          resolve({ success: true, backup });
        } catch (error) {
          console.error('[CAC] Import error:', error);
          alert(`❌ 백업 파일을 읽을 수 없습니다:\n${error.message}`);
          reject(error);
        }
      };

      reader.onerror = (e) => {
        console. error('[CAC] File read error:', e);
        reject(e);
      };

      reader. readAsText(file);
    });
  }

  // ===== 2. 설정 팝업에 백업/복원 버튼 추가 =====
  function addBackupButtonsToSettings() {
    const checkAndAddButtons = setInterval(() => {
      const settingsModal = document.getElementById('autocomplete_settings');
      if (!settingsModal) return;

      // 이미 백업 버튼이 있으면 스킵
      if (document.getElementById('backup-export-btn')) {
        clearInterval(checkAndAddButtons);
        return;
      }

      const leftPane = settingsModal.querySelector('.autocomplete_pane.left_pane');
      if (!leftPane) return;

      // 백업/복원 버튼 컨테이너
      const backupContainer = document.createElement('div');
      backupContainer.style.cssText = 'display:flex;gap:8px;margin-top:16px;border-top:1px solid rgba(115,119,127,0.3);padding-top:16px;';

      // 내보내기 버튼
      const exportBtn = document.createElement('button');
      exportBtn.id = 'backup-export-btn';
      exportBtn.className = 'autocomplete_button';
      exportBtn.textContent = '💾 백업 내보내기';
      exportBtn.onclick = exportBackup;
      exportBtn.style.flex = '1';

      // 불러오기 버튼
      const importBtn = document.createElement('button');
      importBtn.id = 'backup-import-btn';
      importBtn.className = 'autocomplete_button';
      importBtn.textContent = '📂 백업 불러오기';
      importBtn.style.flex = '1';

      // 숨겨진 파일 입력
      const fileInput = document.createElement('input');
      fileInput. type = 'file';
      fileInput.accept = '.json';
      fileInput.style.display = 'none';
      fileInput.onchange = async (e) => {
        const file = e.target.files[0];
        if (file) {
          try {
            await importBackup(file);
            // 페이지 새로고침 (새 데이터 반영)
            setTimeout(() => location.reload(), 1000);
          } catch (err) {
            console.error('[CAC] Import failed:', err);
          }
        }
      };

      importBtn.onclick = () => fileInput.click();

      backupContainer.append(exportBtn, importBtn, fileInput);
      leftPane.appendChild(backupContainer);

      clearInterval(checkAndAddButtons);
    }, 500);

    // 20초 후 포기
    setTimeout(() => clearInterval(checkAndAddButtons), 20000);
  }

  // ===== 2. CSS 스타일 =====
  GM_addStyle(`
/* 팝업 스타일 */
#autocomplete_popup {
  display: flex;
  position: absolute;
  bottom: 48px;
  width: calc(100% - 10px);
  flex-direction: column;
  height: max-content;
  max-height: 150px;
  overflow: hidden scroll !important;
  z-index: 9;
  background: rgb(56, 58, 62) !important;
  border: 0px none !important;
  border-radius: 12px;
}

html #autocomplete_popup .autocomplete_item {
  padding: 8px 28px;
  line-height: normal;
  word-break: keep-all;
  white-space: pre-wrap;
  cursor: pointer;
  user-select: none;
  -webkit-user-select: none;
  font-size: 14px !important;
  color: rgba(255, 255, 255, 0.8) !important;
  font-family: -apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif !important;
}

#autocomplete_popup .autocomplete_item:hover,
#autocomplete_popup .autocomplete_item.selected {
  background: rgb(79, 82, 88) !important;
}

#autocomplete_popup .autocomplete_item:active {
  background: rgb(91, 94, 101) !important;
}

.autocomplete_subtext {
  color: rgba(255, 255, 255, 0.5) !important;
  font-size: 12px !important;
  font-family: -apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif !important;
}

/* 설정 UI 스타일 */
#autocomplete_settings {
  display: none;
  position: fixed;
  top: 32px;
  bottom: 32px;
  left: 32px;
  right: 32px;
  overflow-y: scroll;
  padding: 24px;
  gap: 24px;
  background: rgba(56, 58, 62, 0.875);
  border: 1px solid rgba(115, 119, 127, 0.75);
  border-radius: 12px;
  backdrop-filter: blur(4px);
  z-index: 99999;
}

#autocomplete_settings.opened {
  display: flex;
}

.autocomplete_button {
  display: flex;
  flex-shrink: 0;
  padding: 12px 24px;
  border: 0.8px solid rgba(115, 119, 127, 0.5) !important;
  border-radius: 6px;
  line-height: normal;
  align-items: center;
  justify-content: center;
  word-break: keep-all;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  cursor: pointer;
  user-select: none;
  -webkit-user-select: none;
  background: rgba(0, 0, 0, 0) !important;
  color: rgb(255, 255, 255) !important;
  font-size: 14px !important;
  font-family: -apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif !important;
  transition: background 0.2s cubic-bezier(0.2, 1, 0.5, 0.95);
}

.autocomplete_button:hover,
.autocomplete_button.selected {
  background: rgb(79, 82, 88) !important;
}

.autocomplete_button:active {
  background: rgb(91, 94, 101) !important;
}

.autocomplete_button.left {
  justify-content: flex-start;
}

#backup-export-btn,
#backup-import-btn {
  padding: 12px 24px;
  border: 0.8px solid rgba(115, 119, 127, 0.5) !important;
  border-radius: 6px;
  background: rgba(0, 0, 0, 0) !important;
  color: rgb(255, 255, 255) !important;
  cursor: pointer;
  user-select: none;
  transition: background 0.2s cubic-bezier(0.2, 1, 0.5, 0.95);
  font-weight: bold;
}

#backup-export-btn:hover,
#backup-import-btn:hover {
  background: rgb(79, 82, 88) !important;
}

#backup-export-btn:active,
#backup-import-btn:active {
  background: rgb(91, 94, 101) !important;
}

.autocomplete_pane {
  display: flex;
  width: 100%;
  height: 100%;
  gap: 8px;
  flex-direction: column;
  overflow-y: scroll;
}

.autocomplete_pane.right_pane {
  display: flex;
  width: 100%;
  height: 100%;
  flex-direction: column;
}

/* 오른쪽 부분(right_pane) INPUT 스타일 */
.autocomplete_pane.right_pane input {
  background-color: rgb(21, 22, 25) !important;
  color: rgb(255, 255, 255) !important;
  border: 1.6px solid rgb(115, 119, 127) !important;
  border-radius: 8px !important;
  padding: 12px 16px !important;
  font-size: 14px !important;
  font-family: -apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif !important;
  width: 100% !important;
  box-sizing: border-box !important;
}

.autocomplete_pane.right_pane input:hover {
  background-color: rgb(33, 34, 37) !important;
  border-color: rgb(140, 145, 155) !important;
}

.autocomplete_pane.right_pane input:focus {
  background-color: rgb(33, 34, 37) !important;
  border-color: rgb(167, 171, 180) !important;
}

/* 오른쪽 부분(right_pane) DIV 라벨 스타일 */
.autocomplete_pane.right_pane > div:not(input):not(button) {
  color: rgb(255, 255, 255) !important;
  font-size: 14px !important;
  font-family: -apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif !important;
  font-weight: 400 !important;
  padding: 0px !important;
  margin: 0px !important;
  line-height: 16.8px !important;
}

/* 오른쪽 부분(right_pane) BUTTON 스타일 */
.autocomplete_pane.right_pane button {
  background-color: rgba(0, 0, 0, 0) !important;
  color: rgb(255, 255, 255) !important;
  border: 0.8px solid rgba(115, 119, 127, 0.5) !important;
  border-radius: 6px !important;
  padding: 12px 24px !important;
  font-size: 14px !important;
  font-family: -apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif !important;
  cursor: pointer !important;
  transition: color 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), background-color 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), border-color 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), text-decoration-color 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), fill 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), stroke 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), opacity 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), box-shadow 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), transform 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), filter 0.2s cubic-bezier(0.2, 1, 0.5, 0.95), backdrop-filter 0.2s cubic-bezier(0.2, 1, 0.5, 0.95) !important;
}

.autocomplete_pane.right_pane button:hover {
  background-color: rgb(79, 82, 88) !important;
}

.autocomplete_pane.right_pane button:active {
  background-color: rgb(91, 94, 101) !important;
}

.autocomplete_input {
  display: flex;
  position: relative;
  width: 100%;
  margin: 0;
  outline: none;
  border: 2px solid rgb(115, 119, 127);
  border-radius: 8px;
  padding: 12px 16px;
  line-height: normal;
  align-items: center;
  background: rgb(21, 22, 25);
  color: white;
  cursor: text;
}

.autocomplete_input:hover,
.autocomplete_input:focus {
  background: rgb(33, 34, 37);
}

.autocomplete_input:hover {
  border-color: rgb(140, 145, 155);
}

.autocomplete_input:focus {
  border-color: rgb(167, 171, 180);
}

.autocomplete_textarea {
  display: flex;
  position: relative;
  width: 100%;
  height: 100%;
  margin: 0;
  outline: none;
  border: 2px solid rgb(115, 119, 127);
  border-radius: 8px;
  padding: 12px 16px;
  line-height: normal;
  align-items: center;
  background: rgb(21, 22, 25);
  color: white;
  cursor: text;
}

.autocomplete_textarea:hover,
.autocomplete_textarea:focus {
  background: rgb(33, 34, 37);
}

.autocomplete_textarea:hover {
  border-color: rgb(140, 145, 155);
}

.autocomplete_textarea:focus {
  border-color: rgb(167, 171, 180);
}

.transition {
  transition-property: color, background-color, border-color, text-decoration-color, fill, stroke, opacity, box-shadow, transform, filter, backdrop-filter;
  transition-timing-function: cubic-bezier(0.2, 1, 0.5, 0.95);
  transition-duration: 0.2s;
}
  `);

  // ===== 3. MutationObserver로 right_pane 스타일 강제 적용 =====
  let styleObserverInitialized = false;

  const applyRightPaneStyles = () => {
    const rightPane = document.querySelector('.autocomplete_pane.right_pane');
    if (!rightPane) return;

    // INPUT 요소들
    rightPane.querySelectorAll('input').forEach(input => {
      input.style.backgroundColor = 'rgb(21, 22, 25)';
      input.style.color = 'rgb(255, 255, 255)';
      input.style.border = '1.6px solid rgb(115, 119, 127)';
      input.style.borderRadius = '8px';
      input.style.padding = '12px 16px';
      input.style.fontSize = '14px';
      input.style.fontFamily = '-apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif';
      input.style.width = '100%';
      input.style.boxSizing = 'border-box';
    });

    // DIV 라벨 요소들
    rightPane.querySelectorAll('div').forEach(div => {
      if (div.tagName !== 'INPUT' && div.tagName !== 'BUTTON') {
        if (div.children.length === 0 || div.textContent.trim().length > 0) {
          div.style.color = 'rgb(255, 255, 255)';
          div.style.fontSize = '14px';
          div.style.fontFamily = '-apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif';
          div.style.fontWeight = '400';
          div.style.lineHeight = '16.8px';
        }
      }
    });

    // BUTTON 요소들
    rightPane.querySelectorAll('button').forEach(button => {
      button.style.backgroundColor = 'rgba(0, 0, 0, 0)';
      button.style.color = 'rgb(255, 255, 255)';
      button.style.border = '0.8px solid rgba(115, 119, 127, 0.5)';
      button.style.borderRadius = '6px';
      button.style.padding = '12px 24px';
      button.style.fontSize = '14px';
      button.style.fontFamily = '-apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif';
      button.style.cursor = 'pointer';
    });
  };

  // DOM 변경 감시
  const styleObserver = new MutationObserver(() => {
    applyRightPaneStyles();
  });

  // 옵저버 시작
  const startStyleObserver = () => {
    if (!styleObserverInitialized) {
      styleObserver.observe(document.body, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['style', 'class']
      });
      styleObserverInitialized = true;
      console.log('[CAC] Style observer started');
    }
    applyRightPaneStyles();
  };

  // 초기 적용 및 감시
  startStyleObserver();

  // ===== 4. SOOP font-family 강제 적용 =====
  const applyFontFix = () => {
    const items = document.querySelectorAll('#autocomplete_popup .autocomplete_item');
    items.forEach(item => {
      item.style.fontFamily = '-apple-system, BlinkMacSystemFont, "Malgun Gothic", "맑은 고딕", Helvetica, Arial, sans-serif';
      item.style.fontSize = '14px';
      item.style.color = 'rgba(255, 255, 255, 0.8)';
    });
  };

  // 요소가 생성/변경될 때마다 적용
  const observer = new MutationObserver(() => {
    applyFontFix();
  });

  observer.observe(document.body, { childList: true, subtree: true });

  // 초기 적용
  applyFontFix();
  addBackupButtonsToSettings();
  // ===== 5. 기존 CAC 코드 =====
  const WRAP = location.hostname.includes('chzzk')
    ? "div[class*=live_chatting_input_container]"
    : "#chat_write";
  const INPUT = location.hostname.includes('chzzk')
    ? "pre[contenteditable]"
    : "#write_area[contenteditable='true']";

  var b=(e,{scope:n,equals:s=pe,lazy:o=!0}={})=>{let l={scope:n,equals:s,init:e};l.base=l;let t=Object.create(l);return t.e=98,t.a={get signal(){return ne(t)}},e instanceof Function?(t.r=0,t.u=e,o||(t.e=102,N(t)),t.get=(i=!(t.e&7180))=>{if(i&&N(t),t.e&1)return t.n;throw t.e&2?new Promise((u,r)=>{let p=t.watch(()=>{t.o===void 0?u(t.n):r(t.o),p()})}):t.o},Object.defineProperty(t,"state",{get:()=>({pending:!!(t.e&2)||!(t.e&7180),error:t.o,value:t.n})})):(t.e=105,t.n=e,t.get=()=>t.n,t.set=i=>{J(t,i instanceof Function?i(t.n):i)}),t.watch=i=>{let u=!(t.e&7180);return t.e|=2048,(t.l??=new Set).add(i),u&&N(t),()=>{t.l.delete(i),!t.l.size&&!((t.e&=-2049)&7180)&&W(t)}},t.subscribe=i=>{let u=!(t.e&7180);t.e|=4096;let r={p:i,a:{get signal(){return ne(r)}}};return(t.i??=new Set).add(r),u?N(t):t.e&1&&queueMicrotask(()=>i(t.n,t.a)),()=>{t.i.delete(r),!t.i.size&&!((t.e&=-2049)&7180)&&W(t),r.t!==void 0&&(r.t.abort(),r.t=void 0)}},t};var N=e=>{e.e|=2;let n=++e.r;e.o!==void 0&&(e.o=void 0),e.t!==void 0&&(e.t.abort(),e.t=void 0);try{let s=e.u((o,l=!1)=>{if(n!==e.r)throw void 0;if(o!==e&&(e.scope!==void 0&&(o=l?e.scope(o):e.scope.get(o)),e.e&7180)){(e.c??=new Set).add(o),o.s??=new Set;let t=!!(e.e&32)&&!(o.e&7180);o.e|=1024;let i=o.get(t);return o.s.add(e),i}return o.get()},e.a);me(s)?(s.then(o=>{n===e.r&&J(e,o)},o=>{n===e.r&&te(e,o)}),z(e)):J(e,s)}catch(s){te(e,s)}},J=(e,n)=>{e.e=e.e&-3|1;let s=e.n;!Object.is(n,s)&&!e.equals(n,s)&&(e.n=n,z(e),(e.e&4160)===4160&&(e.e&=-65,de(()=>{if((e.e&4097)===4097)for(let o of e.i)o.t!==void 0&&(o.t.abort(),o.t=void 0),queueMicrotask(()=>o.p(e.n,o.a));e.e|=64})),oe(e))},te=(e,n)=>{e.e&=-4,Object.is(e.o,n)||(e.o=n,z(e),oe(e))},z=e=>{if(e.e&2048)for(let n of e.l)n();e.e&=-129},oe=e=>{if((e.e&1056)===1056){se(e);for(let n of e.s)n.e|=128;for(let n=q.length;n--;){let s=q[n];if(s.e&128)if(N(s),(s.e&1152)===1024){for(let o of s.s)o.e|=128;s.e&=-1025,s.s.clear()}else s.e&=-129;s.e|=32}q=[]}},se=e=>{for(let n of e.s)n.e&32&&n.e&7172&&(n.e&=-33,n.e&1024&&se(n),q.push(n))},W=e=>{e.t!==void 0&&(e.t.abort(),e.t=void 0);for(let n of e.c)n.s.delete(e)&&!n.s.size&&!((e.e&=-1025)&7180)&&W(n);e.c.clear()},ne=e=>{let n=(e.t??=new AbortController).signal;if(n.then===void 0){let s=[];n.then=o=>{n.aborted?o():s.push(o)},n.addEventListener("abort",()=>{for(let o of s)o()},{once:!0,passive:!0})}return n},pe=()=>!1,me=e=>typeof e?.then=="function",q=[],A=[],de=e=>{A.length===0&&queueMicrotask(()=>{let n=A;A=[];for(let s of n)s()}),A.push(e)};var K=["\u3131","\u3132","\u3134","\u3137","\u3138","\u3139","\u3141","\u3142","\u3143","\u3145","\u3146","\u3147","\u3148","\u3149","\u314A","\u314B","\u314C","\u314D","\u314E"],ge=new Set(K.map(e=>e.charCodeAt(0)));var fe=["\u314F","\u3150","\u3151","\u3152","\u3153","\u3154","\u3155","\u3156","\u3157","\u3157\u314F","\u3157\u3150","\u3157\u3163","\u315B","\u315C","\u315C\u3153","\u315C\u3154","\u315C\u3163","\u3160","\u3161","\u3161\u3163","\u3163"];var be=["","\u3131","\u3132","\u3131\u3145","\u3134","\u3134\u3148","\u3134\u314E","\u3137","\u3139","\u3139\u3131","\u3139\u3141","\u3139\u3142","\u3139\u3145","\u3139\u314C","\u3139\u314D","\u3139\u314E","\u3141","\u3142","\u3142\u3145","\u3145","\u3146","\u3147","\u3148","\u314A","\u314B","\u314C","\u314D","\u314E"];var F=e=>{let n="",s=e.length;for(let o=0;o<s;o+=1){let l=e.charCodeAt(o),t=l-44032|0;if(t<0||t>=11172){n+=String.fromCharCode(l);continue}let i=K[t/588|0],u=fe[(t%588|0)/28|0],r=be[t%28|0];n+=i+u+r}return n},ae=e=>{let n="",s=e.length;for(let o=0;o<s;o+=1){let l=e.charCodeAt(o);if(ge.has(l)||l===10||l===32){n+=String.fromCharCode(l);continue}let t=l-44032|0;if(t<0||t>=11172)continue;let i=K[t/588|0];n+=i}return n};var xe=e=>e.nodeType===Node.ELEMENT_NODE,U=(e,n,s=document.body)=>{let o=new WeakMap,l=r=>{let p=[],f=v=>{p.push(v)};o.set(r,p),n(r,f)},t=r=>{let p=o.get(r);o.delete(r);for(let f of p)f()},i=(r,p)=>{if(xe(r)){r.matches(e)&&p(r);for(let f of r.querySelectorAll(e))p(f)}};for(let r of s.querySelectorAll(e))l(r);let u=new MutationObserver(r=>{for(let p of r){for(let f of p.removedNodes)i(f,t);for(let f of p.addedNodes)i(f,l)}});return u.observe(s,{childList:!0,subtree:!0}),u};var re=e=>{let n=!0,s=()=>{n&&(e(),requestAnimationFrame(s))};return requestAnimationFrame(s),()=>{n=!1}};localStorage.getItem("ac.settings")===null&&localStorage.setItem("ac.settings",JSON.stringify({cooltime:2*1e3,nextLyricsCount:3,wsRemovalProb:.02,conRemovalProb:.02,xCoord:0,yCoord:0,opacity:1}));localStorage.getItem("ac.templates")===null&&localStorage.setItem("ac.templates",JSON.stringify([]));var{cooltime:he=2*1e3,nextLyricsCount:ve=3,wsRemovalProb:Ve=.02,conRemovalProb:Be=.02,xCoord:XC=0,yCoord:YC=0,opacity:OP=1}=JSON.parse(localStorage.getItem("ac.settings")??"{}"),ye=JSON.parse(localStorage.getItem("ac.templates")??"[]"),P=b(Math.max(he,2*1e3)),M=b(ve),O=b(Ve),$=b(Be),xc=b(XC),yc=b(YC),op=b(OP),C=b(ye),le=b(0),Q=b(!1),j=b(!1),Se=b(e=>ke(e(C)));re(()=>le.set(performance.now()));var ie=e=>{let n=e.match(/<(.+?)>$/);return n===null?null:n[1]},Z=e=>e.match(/[0-9]+|[a-zA-Z]+|[ㄱ-ㅎㅏ-ㅣ가-힣]+|\S/g)??[],ce=e=>e.toLowerCase().replace(/\s/g," "),Ee=(e,n)=>e.replace(/\s/g,s=>Math.random()<n?"":s),Ce=(e,n)=>e.replace(/[ㄱ-ㅎㅏ-ㅣ !,.;()\[\]<>?^Vv~→←↑↓↖↗↘↙⬈⬉⬊⬋⬌⬅⬆⬇⮕⭠⭡⭢⭣]{4,}/g,s=>s.replace(/./g,o=>Math.random()<n?"":o)),we=e=>{let n=e.split(/\s*\|\s*/g).filter(t=>t.length>0).map(t=>{let i=t.split(":");return{text:i[0],prob:Number.parseFloat(i[1]??-100)/100}});if(n.length===0)return null;let s=0,o=1;for(let t of n)t.prob<0?s+=1:o-=t.prob=Math.min(t.prob,o);let l=o/s;for(let t of n)t.prob<0&&(t.prob=l);return n},Te=({text:e})=>{let n=ce(e),s=F(n);return{split:s,tokens:Z(s),chosungTokens:Z(ae(n))}},Ie=e=>{let n=Math.random(),s=0;for(let{text:o,prob:l}of e)if(s+=l,n<=s)return o;return e[0].text},ke=e=>{let n=[];for(let{title:s,text:o}of e){let l=o.trim().split(/\n+/g),t=l.length;for(let i=0;i<t;i+=1){let u=l[i].trim();if(u.length===0)continue;let r=we(u);if(r===null)continue;let p=r[0].text,f=r.map(Te),v={title:s,texts:r,mainText:p,transformations:f,next:null};i>0&&(n[n.length-1].next=v),n.push(v)}}return n},G=(e,n,s)=>{let o=e.length,l=n.length;if(o>l||o<=0)return null;let t=!1,i=0,u=0,r=0,p=e[0];for(let f=0;f+(o-r)<=l;f++){let v=n[f],B=v.length;if(p.startsWith(v))if(t=!0,p.length>B)p=p.slice(B),u+=1;else if(++r<o)p=e[r];else return i*1e4+f*100+u;else{if(v.startsWith(p)&&r+1>=o)return i*1e4+f*100+u;s?(t=!1,u=0,r=0,p=e[0]):t&&(t=!1,i+=1)}}return null},_e=(e,n)=>{let s=e.length,o=n.length;if(s>o||s<=0)return null;let l=!1,t=0,i=0;for(let u=0;u+(s-i)<=o;u++)if(e[i]===n[u]){if(l=!0,++i>=s)return t*1e4+u*100}else l&&(l=!1,t+=1);return null},Le=(e,n,s)=>{if(n.length===0)return null;if(/\s/.test(n[0]))return[];let o=[],l=[],t=[],i=F(ce(n)),u=Z(i);for(let r of e){let{transformations:p}=r,f=ie(r.title)===s?0:1,v=1e9,B=1e9,T=1e9;for(let{split:_,tokens:L,chosungTokens:c}of p){let d=G(u,L,!0)??G(u,L,!1)??1e9,h=G(u,c,!0)??G(u,c,!1)??1e9,y=_e(i,_)??1e9;v=Math.min(v,d),B=Math.min(B,h),T=Math.min(T,y)}v<1e9&&o.push({template:r,categoryPenalty:f,score:v}),B<1e9&&l.push({template:r,categoryPenalty:f,score:B}),T<1e9&&t.push({template:r,categoryPenalty:f,score:T})}return o.length===0&&(o=l),o.length===0&&(o=t),o.sort((r,p)=>r.categoryPenalty-p.categoryPenalty||r.score-p.score),o.map(r=>r.template)},Ne=({$popupElm:e,$inputElm:n,$text:s,$selection:o,$lastCompletionTime:l,$lastCompletion:t,$lastCompletionCategory:i},u)=>{let r=a=>{let g=n.get();g!==null&&(g.textContent=a,s.set(a))},p=()=>{let a=n.get();if(a===null)return;let g=document.createRange();g.selectNodeContents(a);let x=window.getSelection();x!==null&&(x.removeAllRanges(),x.addRange(g))},f=()=>{let a=n.get();if(a===null||c.get()!==null)return;let g=y.get()[o.get()],x=O.get(),m=$.get();a.focus(),p(),requestAnimationFrame(()=>{r(Ce(Ee(Ie(g.texts),x),m)),a.focus(),p(),o.set(0),l.set(performance.now()),t.set(g),i.set(ie(g.title))})},v=({target:a})=>{o.set(S.get().indexOf(a)),f()},B=()=>{j.set(!0)},T=(a,g)=>{if(a===null)return[];let x=[],m=a.next;for(let E=0;E<g&&m!==null;E+=1)x.push(m),m=m.next;return x},_=()=>{let a=document.createElement("div");return a.innerHTML="\uC124\uC815",a.className="autocomplete_item",a.addEventListener("click",B),a},L=(a,g)=>{let{title:x,mainText:m,next:E}=a,V=document.createElement("div"),I=E?E.mainText:"(\uB05D)",D=g?`\u{1F512}(${g}\uCD08) `:"";return V.innerHTML=`${D}${m} <span class="autocomplete_subtext">\u2192 ${I} @ [${x}]</span>`,V.className="autocomplete_item",V.addEventListener("click",v),V},c=b(a=>{let g=a(le),x=a(l),m=a(P),V=x+m-g;return V<=0?null:(V/1e3).toFixed(1)}),d=b(a=>/^\s$/.test(a(s))),h=b(a=>T(a(t),a(M))),y=b(a=>{if(a(Q))return[];let g=Le(a(Se),a(s),a(i));if(g===null)return a(h);let x=a(t);return g.filter(m=>m!==x).slice(0,10)}),S=b(a=>{if(a(d))return[_()];let g=a(c);return a(y).map(m=>L(m,g))}),w=b(a=>a(S)[a(o)]);return u(y.subscribe(()=>{o.set(0)})),u(S.subscribe((a,{signal:g})=>{e.get().append(...a),g.then(()=>{for(let m of a)m.remove()})})),u(w.subscribe((a,{signal:g})=>{a!==void 0&&(a.classList.add("selected"),a.scrollIntoView({behavior:"instant",block:"nearest"}),g.then(()=>{a.classList.remove("selected")}))})),{handleControlKey:a=>{if(j.get())return;let{ctrlKey:g,altKey:x,metaKey:m,shiftKey:E,key:V,isComposing:I}=a;if(V==="Dead"||V==="Unidentified"||V==="Enter"||g||x||m||E||I)return;let D=d.get(),X=Q.get();if(V==="Escape"&&!D){Q.set(!X);return}if(X)return;let R=y.get().length,Y=o.get(),H=R===0,ee=ue=>{o.set((ue%R+R)%R)};if(V==="ArrowDown"){if(H)return;ee(Y+1)}else if(V==="ArrowUp"){if(H)return;ee(Y-1)}else if(V==="Tab")D?B():H||f();else return;a.preventDefault(),a.stopPropagation()}}};
})();
