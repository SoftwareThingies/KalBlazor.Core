function getTooltipState() {
    return window.__kalTooltipState;
}

function invokeHost(state, methodName, ...args) {
    if (state.dotNetRef === null) {
        return;
    }

    state.dotNetRef.invokeMethodAsync(methodName, ...args);
}

function getViewportSize() {
    const documentElement = document.documentElement;

    return {
        width: documentElement.clientWidth,
        height: window.innerHeight || documentElement.clientHeight
    };
}

function positionTooltipElement(element, clientX, clientY, offsetX, offsetY) {
    if (!(element instanceof HTMLElement) || !element.isConnected) {
        return;
    }

    const viewportPadding = 8;
    const rect = element.getBoundingClientRect();
    const viewport = getViewportSize();
    const preferredLeft = clientX + offsetX;
    const preferredTop = clientY + offsetY;
    const fallbackLeft = clientX - offsetX - rect.width;
    const fallbackTop = clientY - offsetY - rect.height;
    const maxLeft = Math.max(viewportPadding, viewport.width - rect.width - viewportPadding);
    const maxTop = Math.max(viewportPadding, viewport.height - rect.height - viewportPadding);

    const left = preferredLeft + rect.width > viewport.width - viewportPadding
        ? Math.max(viewportPadding, Math.min(fallbackLeft, maxLeft))
        : Math.max(viewportPadding, Math.min(preferredLeft, maxLeft));

    const top = preferredTop + rect.height > viewport.height - viewportPadding
        ? Math.max(viewportPadding, Math.min(fallbackTop, maxTop))
        : Math.max(viewportPadding, Math.min(preferredTop, maxTop));

    element.style.left = `${left}px`;
    element.style.top = `${top}px`;
    element.style.visibility = "visible";
}

function getPrimaryTouch(event) {
    if (event.touches.length > 0) {
        return event.touches[0];
    }

    if (event.changedTouches.length > 0) {
        return event.changedTouches[0];
    }

    return null;
}

function hasRecentTouchInteraction(state) {
    return Date.now() - state.lastTouchTimestamp < 750;
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

    if (state.animationFrameId !== 0 || state.activeTrigger === null) {
        return;
    }

    state.animationFrameId = window.requestAnimationFrame(() => {
        state.animationFrameId = 0;

        if (state.activeTrigger === null) {
            return;
        }

        positionTooltipElement(
            state.tooltipElement,
            state.pendingClientX,
            state.pendingClientY,
            state.offsetX,
            state.offsetY);
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
        lastTouchTimestamp: 0,
        tooltipElement: null,
        offsetX: 0,
        offsetY: 0,
        handlers: null
    };

    window.__kalTooltipState = state;

    const hideTooltip = () => {
        state.activeTrigger = null;
        state.tooltipElement = null;
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
        if (hasRecentTouchInteraction(state)) {
            return;
        }

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
        if (hasRecentTouchInteraction(state)) {
            return;
        }

        if (state.activeTrigger === null) {
            return;
        }

        setPendingMove(state, event.clientX, event.clientY);
    };

    const mouseOutHandler = event => {
        if (hasRecentTouchInteraction(state)) {
            return;
        }

        if (state.activeTrigger === null) {
            return;
        }

        const relatedTarget = event.relatedTarget;

        if (relatedTarget instanceof Node && state.activeTrigger.contains(relatedTarget)) {
            return;
        }

        hideTooltip();
    };

    const touchStartHandler = event => {
        const trigger = getTooltipTrigger(event.target);
        const touch = getPrimaryTouch(event);

        state.lastTouchTimestamp = Date.now();

        if (trigger === null || touch === null) {
            hideTooltip();
            return;
        }

        if (state.activeTrigger === trigger) {
            hideTooltip();
            return;
        }

        showTooltip(trigger, touch);
    };

    const scrollHandler = () => hideTooltip();
    const resizeHandler = () => hideTooltip();
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
        touchStartHandler,
        scrollHandler,
        resizeHandler,
        blurHandler,
        visibilityChangeHandler
    };

    document.addEventListener("mouseover", mouseOverHandler, true);
    document.addEventListener("mousemove", mouseMoveHandler, true);
    document.addEventListener("mouseout", mouseOutHandler, true);
    document.addEventListener("touchstart", touchStartHandler, true);
    window.addEventListener("scroll", scrollHandler, true);
    window.addEventListener("resize", resizeHandler);
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
    document.removeEventListener("touchstart", state.handlers.touchStartHandler, true);
    window.removeEventListener("scroll", state.handlers.scrollHandler, true);
    window.removeEventListener("resize", state.handlers.resizeHandler);
    window.removeEventListener("blur", state.handlers.blurHandler);
    document.removeEventListener("visibilitychange", state.handlers.visibilityChangeHandler);
    delete window.__kalTooltipState;
}

export function setKalTooltipElement(tooltipElement, clientX, clientY, offsetX, offsetY) {
    const state = getTooltipState();

    if (!state) {
        return;
    }

    state.tooltipElement = tooltipElement;
    state.offsetX = offsetX;
    state.offsetY = offsetY;
    state.pendingClientX = state.activeTrigger === null ? clientX : state.pendingClientX;
    state.pendingClientY = state.activeTrigger === null ? clientY : state.pendingClientY;

    positionTooltipElement(
        state.tooltipElement,
        state.pendingClientX,
        state.pendingClientY,
        state.offsetX,
        state.offsetY);
}

export function clearKalTooltipElement() {
    const state = getTooltipState();

    if (!state) {
        return;
    }

    state.tooltipElement = null;
}
