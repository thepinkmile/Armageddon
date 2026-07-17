window.playMatchSiren = function () {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();

    const sweepCount = 9;
    const sweepDuration = 0.8;
    const totalDuration = sweepCount * sweepDuration;

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = 'sawtooth';
    osc.connect(gain);
    gain.connect(ctx.destination);

    gain.gain.setValueAtTime(0.4, ctx.currentTime);
    gain.gain.linearRampToValueAtTime(0, ctx.currentTime + totalDuration);

    for (let i = 0; i < sweepCount; i++) {
        const t = ctx.currentTime + i * sweepDuration;
        osc.frequency.setValueAtTime(660, t);
        osc.frequency.linearRampToValueAtTime(1320, t + sweepDuration * 0.5);
        osc.frequency.linearRampToValueAtTime(660, t + sweepDuration);
    }

    osc.start(ctx.currentTime);
    osc.stop(ctx.currentTime + totalDuration);
};
