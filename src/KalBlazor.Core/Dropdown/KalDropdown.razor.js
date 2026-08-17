const viewportPadding = 8;
const defaultGap = 8;

function getState(panel) {
    return panel.__kalDropdownState;
}

function positionPanel(panel, trigger) {
    if (!(panel instanceof HTMLElement)
        || !(trigger instanceof HTMLElement)
        || !panel.isConnected
        || !trigger.isConnected) {
        return;
    }

    panel.style.maxHeight = "none";
    panel.style.overflowY = "visible";

    const triggerRect = trigger.getBoundingClientRect();
    const panelRect = panel.getBoundingClientRect();
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    const gap = Number.parseFloat(getComputedStyle(panel).marginTop) || defaultGap;
    const spaceBelow = Math.max(0, viewportHeight - triggerRect.bottom - viewportPadding - gap);
    const spaceAbove = Math.max(0, triggerRect.top - viewportPadding - gap);
    const fitsBelow = panelRect.height <= spaceBelow;
    const opensAbove = !fitsBelow;
    const availableHeight = opensAbove ? spaceAbove : spaceBelow;

    panel.style.maxHeight = `${Math.max(0, availableHeight)}px`;
    panel.style.overflowY = "auto";

    if (opensAbove) {
        panel.style.top = "auto";
        panel.style.bottom = `calc(100% + ${gap}px)`;
        panel.style.marginTop = "0px";
        panel.style.marginBottom = "0px";
    } else {
        panel.style.top = "";
        panel.style.bottom = "";
        panel.style.marginTop = "";
        panel.style.marginBottom = "";
    }
}

export function positionKalDropdownPanel(panel, trigger) {
    if (!(panel instanceof HTMLElement) || !(trigger instanceof HTMLElement)) {
        return;
    }

    const existingState = getState(panel);

    if (existingState) {
        existingState.trigger = trigger;
        positionPanel(panel, trigger);
        return;
    }

    const state = {
        trigger,
        animationFrameId: 0,
        update: () => {
            if (state.animationFrameId !== 0) {
                return;
            }

            state.animationFrameId = window.requestAnimationFrame(() => {
                state.animationFrameId = 0;

                if (!panel.isConnected) {
                    disposeKalDropdownPanel(panel);
                    return;
                }

                positionPanel(panel, state.trigger);
            });
        }
    };

    panel.__kalDropdownState = state;
    window.addEventListener("resize", state.update);
    window.addEventListener("scroll", state.update, true);
    positionPanel(panel, trigger);
}

export function disposeKalDropdownPanel(panel) {
    const state = panel?.__kalDropdownState;

    if (!state) {
        return;
    }

    if (state.animationFrameId !== 0) {
        window.cancelAnimationFrame(state.animationFrameId);
    }

    window.removeEventListener("resize", state.update);
    window.removeEventListener("scroll", state.update, true);
    delete panel.__kalDropdownState;
}
