function getTooltipState() {
    return window.__kalTooltipState;
}

function invokeHost(state, methodName, ...args) {
    if (state.dotNetRef === null) {
        return;
    }

    state.dotNetRef.invokeMethodAsync(methodName, ...args);
}

function getTooltipTrigger(target) {
    if (!(target instanceof Element)) {
        return null;
    }

    const trigger = target.closest("[data-kal-tooltip]");
    return trigger instanceof Element ? trigger : null;
}

function setPendingMove(state, clientX, clientY) {
    state.pendingClientX = clientX;
    state.pendingClientY = clientY;

    if (state.animationFrameId !== 0 || state.dotNetRef === null || state.activeTrigger === null) {
        return;
    }

    state.animationFrameId = window.requestAnimationFrame(() => {
        state.animationFrameId = 0;

        if (state.dotNetRef === null || state.activeTrigger === null) {
            return;
        }

        invokeHost(state, "UpdateTooltipPosition", state.pendingClientX, state.pendingClientY);
    });
}

function clearPendingMove(state) {
    if (state.animationFrameId === 0) {
        return;
    }

    window.cancelAnimationFrame(state.animationFrameId);
    state.animationFrameId = 0;
}

export function initializeKalTooltips(dotNetRef) {
    const existingState = window.__kalTooltipState;

    if (existingState) {
        existingState.dotNetRef = dotNetRef;
        return;
    }

    const state = {
        activeTrigger: null,
        dotNetRef,
        pendingClientX: 0,
        pendingClientY: 0,
        animationFrameId: 0,
        handlers: null
    };

    window.__kalTooltipState = state;

    const hideTooltip = () => {
        state.activeTrigger = null;
        clearPendingMove(state);
        invokeHost(state, "HideTooltip");
    };

    const showTooltip = (trigger, event) => {
        const tooltipText = trigger.getAttribute("data-kal-tooltip");
        const tooltipClass = trigger.getAttribute("data-kal-tooltip-class");

        if (!tooltipText || !tooltipText.trim()) {
            hideTooltip();
            return;
        }

        state.activeTrigger = trigger;
        state.pendingClientX = event.clientX;
        state.pendingClientY = event.clientY;
        invokeHost(state, "ShowTooltip", tooltipText, tooltipClass, event.clientX, event.clientY);
    };

    const mouseOverHandler = event => {
        const trigger = getTooltipTrigger(event.target);

        if (trigger === null) {
            hideTooltip();
            return;
        }

        if (state.activeTrigger === trigger) {
            setPendingMove(state, event.clientX, event.clientY);
            return;
        }

        showTooltip(trigger, event);
    };

    const mouseMoveHandler = event => {
        if (state.activeTrigger === null) {
            return;
        }

        setPendingMove(state, event.clientX, event.clientY);
    };

    const mouseOutHandler = event => {
        if (state.activeTrigger === null) {
            return;
        }

        const relatedTarget = event.relatedTarget;

        if (relatedTarget instanceof Node && state.activeTrigger.contains(relatedTarget)) {
            return;
        }

        hideTooltip();
    };

    const blurHandler = () => hideTooltip();
    const visibilityChangeHandler = () => {
        if (document.visibilityState !== "visible") {
            hideTooltip();
        }
    };

    state.handlers = {
        mouseOverHandler,
        mouseMoveHandler,
        mouseOutHandler,
        blurHandler,
        visibilityChangeHandler
    };

    document.addEventListener("mouseover", mouseOverHandler, true);
    document.addEventListener("mousemove", mouseMoveHandler, true);
    document.addEventListener("mouseout", mouseOutHandler, true);
    window.addEventListener("blur", blurHandler);
    document.addEventListener("visibilitychange", visibilityChangeHandler);
}

export function disposeKalTooltips() {
    const state = getTooltipState();

    if (!state || state.handlers === null) {
        return;
    }

    clearPendingMove(state);
    state.activeTrigger = null;
    state.dotNetRef = null;
    document.removeEventListener("mouseover", state.handlers.mouseOverHandler, true);
    document.removeEventListener("mousemove", state.handlers.mouseMoveHandler, true);
    document.removeEventListener("mouseout", state.handlers.mouseOutHandler, true);
    window.removeEventListener("blur", state.handlers.blurHandler);
    document.removeEventListener("visibilitychange", state.handlers.visibilityChangeHandler);
    delete window.__kalTooltipState;
}
