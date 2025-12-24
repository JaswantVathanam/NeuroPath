// Sound Therapy Audio Manager
// Clean, realistic procedural sounds - no external dependencies

class SoundTherapyManager {
    constructor() {
        this.audioContext = null;
        this.activeSounds = new Map();
        this.masterGain = null;
        this.isInitialized = false;
    }

    async initialize() {
        if (this.isInitialized) return;
        
        try {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            this.masterGain = this.audioContext.createGain();
            this.masterGain.connect(this.audioContext.destination);
            this.masterGain.gain.value = 0.8;
            this.isInitialized = true;
        } catch (e) {
            console.error('Failed to initialize audio context:', e);
        }
    }

    async resume() {
        if (this.audioContext && this.audioContext.state === 'suspended') {
            await this.audioContext.resume();
        }
    }

    // ============ RAIN - Pro Quality Ambient Rain ============
    // Pure filtered noise approach - NO individual drops (they cause the harsh sounds)
    // Professional rain samples are 100% shaped noise textures
    createRain(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.7;
        gainNode.connect(this.masterGain);
        
        const sampleRate = this.audioContext.sampleRate;
        
        // =====================================================
        // PURE NOISE-BASED RAIN - No individual drops
        // This is how professional ambient rain is made
        // =====================================================
        
        const createRainTexture = (config) => {
            const duration = 12; // Long buffer for seamless ambient loop
            const buffer = this.audioContext.createBuffer(2, duration * sampleRate, sampleRate);
            
            for (let ch = 0; ch < 2; ch++) {
                const data = buffer.getChannelData(ch);
                
                // Pink noise generation (smoothest natural noise)
                let b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
                
                for (let i = 0; i < data.length; i++) {
                    const white = Math.random() * 2 - 1;
                    
                    // Paul Kellet's pink noise algorithm
                    b0 = 0.99886 * b0 + white * 0.0555179;
                    b1 = 0.99332 * b1 + white * 0.0750759;
                    b2 = 0.96900 * b2 + white * 0.1538520;
                    b3 = 0.86650 * b3 + white * 0.3104856;
                    b4 = 0.55000 * b4 + white * 0.5329522;
                    b5 = -0.7616 * b5 - white * 0.0168980;
                    
                    const pink = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362) * 0.11;
                    b6 = white * 0.115926;
                    
                    // Very slow, gentle amplitude modulation
                    const t = i / sampleRate;
                    const mod = 0.92 + 0.08 * Math.sin(t * 0.2 + ch) * Math.sin(t * 0.13);
                    
                    data[i] = pink * mod;
                }
                
                // Smooth the buffer to remove any harsh transients
                for (let pass = 0; pass < 2; pass++) {
                    for (let i = 2; i < data.length - 2; i++) {
                        data[i] = (data[i-2] + data[i-1] + data[i] + data[i+1] + data[i+2]) / 5;
                    }
                }
            }
            
            const source = this.audioContext.createBufferSource();
            source.buffer = buffer;
            source.loop = true;
            
            // Bandpass filter to shape the rain frequency
            const filter = this.audioContext.createBiquadFilter();
            filter.type = 'bandpass';
            filter.frequency.value = config.freq;
            filter.Q.value = config.q;
            
            // Additional smoothing lowpass
            const smoother = this.audioContext.createBiquadFilter();
            smoother.type = 'lowpass';
            smoother.frequency.value = config.smoothFreq || 8000;
            smoother.Q.value = 0.5;
            
            const gain = this.audioContext.createGain();
            gain.gain.value = config.gain;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = config.pan || 0;
            
            source.connect(filter);
            filter.connect(smoother);
            smoother.connect(gain);
            gain.connect(pan);
            pan.connect(gainNode);
            source.start();
            
            return source;
        };
        
        // Layer 1: Deep rain body - the warmth and fullness
        const deepRain = createRainTexture({
            freq: 400,
            q: 0.6,
            gain: 0.28,
            smoothFreq: 2000,
            pan: 0
        });
        
        // Layer 2: Main rain character - the "shhhhh" sound
        const mainRain = createRainTexture({
            freq: 1200,
            q: 0.5,
            gain: 0.35,
            smoothFreq: 4000,
            pan: 0
        });
        
        // Layer 3: Rain presence - upper mids
        const presenceRain = createRainTexture({
            freq: 2500,
            q: 0.6,
            gain: 0.18,
            smoothFreq: 5000,
            pan: 0
        });
        
        // Layer 4: Rain air - high frequency shimmer
        const airRain = createRainTexture({
            freq: 4500,
            q: 0.7,
            gain: 0.10,
            smoothFreq: 7000,
            pan: 0
        });
        
        // Layer 5: Left ambient detail
        const leftDetail = createRainTexture({
            freq: 1800,
            q: 0.8,
            gain: 0.12,
            smoothFreq: 4000,
            pan: -0.5
        });
        
        // Layer 6: Right ambient detail
        const rightDetail = createRainTexture({
            freq: 2200,
            q: 0.8,
            gain: 0.12,
            smoothFreq: 4000,
            pan: 0.5
        });
        
        // Layer 7: Sub bass rumble (very low)
        const rumbleDuration = 10;
        const rumbleBuffer = this.audioContext.createBuffer(2, rumbleDuration * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = rumbleBuffer.getChannelData(ch);
            let lastValue = 0;
            
            for (let i = 0; i < data.length; i++) {
                const white = Math.random() * 2 - 1;
                // Brown noise for deep rumble
                lastValue = (lastValue + 0.02 * white) / 1.02;
                data[i] = lastValue * 3;
            }
            
            // Heavy smoothing
            for (let pass = 0; pass < 5; pass++) {
                for (let i = 1; i < data.length - 1; i++) {
                    data[i] = (data[i-1] + data[i] + data[i+1]) / 3;
                }
            }
        }
        
        const rumbleSource = this.audioContext.createBufferSource();
        rumbleSource.buffer = rumbleBuffer;
        rumbleSource.loop = true;
        
        const rumbleFilter = this.audioContext.createBiquadFilter();
        rumbleFilter.type = 'lowpass';
        rumbleFilter.frequency.value = 200;
        
        const rumbleGain = this.audioContext.createGain();
        rumbleGain.gain.value = 0.15;
        
        rumbleSource.connect(rumbleFilter);
        rumbleFilter.connect(rumbleGain);
        rumbleGain.connect(gainNode);
        rumbleSource.start();
        
        // =====================================================
        // INTENSITY VARIATION - Subtle, smooth changes
        // =====================================================
        
        let variationInterval;
        const varyIntensity = () => {
            if (!this.activeSounds.has('Rain')) return;
            
            const now = this.audioContext.currentTime;
            // Very subtle variation
            const newGain = 0.85 + Math.random() * 0.15;
            gainNode.gain.linearRampToValueAtTime(volume * 0.7 * newGain, now + 4);
            
            variationInterval = setTimeout(varyIntensity, 6000 + Math.random() * 4000);
        };
        
        variationInterval = setTimeout(varyIntensity, 5000);
        
        return {
            source: [deepRain, mainRain, presenceRain, airRain, leftDetail, rightDetail, rumbleSource],
            gain: gainNode,
            type: 'generated',
            cleanup: () => clearTimeout(variationInterval)
        };
    }

    // ============ OCEAN WAVES - Clean rolling waves ============
    createOceanWaves(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.5;
        gainNode.connect(this.masterGain);
        
        let waveTimeouts = [];
        
        const createWave = () => {
            if (!this.activeSounds.has('Ocean Waves')) return;
            
            const sampleRate = this.audioContext.sampleRate;
            const duration = 3 + Math.random() * 2;
            const bufferSize = Math.floor(duration * sampleRate);
            const buffer = this.audioContext.createBuffer(2, bufferSize, sampleRate);
            
            for (let channel = 0; channel < 2; channel++) {
                const data = buffer.getChannelData(channel);
                for (let i = 0; i < bufferSize; i++) {
                    const t = i / sampleRate;
                    // Wave shape: rise then fall
                    const envelope = Math.sin(Math.PI * t / duration);
                    // Filtered noise
                    const noise = Math.random() * 2 - 1;
                    data[i] = noise * envelope * 0.3;
                }
            }
            
            const source = this.audioContext.createBufferSource();
            source.buffer = buffer;
            
            const filter = this.audioContext.createBiquadFilter();
            filter.type = 'lowpass';
            filter.frequency.value = 400 + Math.random() * 200;
            
            const waveGain = this.audioContext.createGain();
            waveGain.gain.value = 0.4 + Math.random() * 0.2;
            
            source.connect(filter);
            filter.connect(waveGain);
            waveGain.connect(gainNode);
            
            source.start();
            
            waveTimeouts.push(setTimeout(createWave, (duration * 0.7) * 1000));
        };
        
        createWave();
        waveTimeouts.push(setTimeout(createWave, 1500));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => waveTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ FOREST - Clean bird songs ============
    createForest(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.5;
        gainNode.connect(this.masterGain);
        
        let birdTimeouts = [];
        
        // Bird type 1: Simple chirp
        const chirp = () => {
            if (!this.activeSounds.has('Forest')) return;
            
            const freq = 2500 + Math.random() * 1500;
            const osc = this.audioContext.createOscillator();
            osc.frequency.value = freq;
            osc.type = 'sine';
            
            const birdGain = this.audioContext.createGain();
            const now = this.audioContext.currentTime;
            birdGain.gain.setValueAtTime(0.2, now);
            birdGain.gain.exponentialRampToValueAtTime(0.001, now + 0.1);
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.6;
            
            osc.connect(birdGain);
            birdGain.connect(pan);
            pan.connect(gainNode);
            
            osc.start(now);
            osc.stop(now + 0.12);
            
            birdTimeouts.push(setTimeout(chirp, 2000 + Math.random() * 4000));
        };
        
        // Bird type 2: Two-note call
        const twoNote = () => {
            if (!this.activeSounds.has('Forest')) return;
            
            const highFreq = 1800 + Math.random() * 400;
            const lowFreq = highFreq * 0.75;
            const now = this.audioContext.currentTime;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.4;
            pan.connect(gainNode);
            
            // First note
            const osc1 = this.audioContext.createOscillator();
            osc1.frequency.value = highFreq;
            osc1.type = 'sine';
            const g1 = this.audioContext.createGain();
            g1.gain.setValueAtTime(0.18, now);
            g1.gain.exponentialRampToValueAtTime(0.001, now + 0.2);
            osc1.connect(g1);
            g1.connect(pan);
            osc1.start(now);
            osc1.stop(now + 0.25);
            
            // Second note
            const osc2 = this.audioContext.createOscillator();
            osc2.frequency.value = lowFreq;
            osc2.type = 'sine';
            const g2 = this.audioContext.createGain();
            g2.gain.setValueAtTime(0.15, now + 0.3);
            g2.gain.exponentialRampToValueAtTime(0.001, now + 0.5);
            osc2.connect(g2);
            g2.connect(pan);
            osc2.start(now + 0.3);
            osc2.stop(now + 0.55);
            
            birdTimeouts.push(setTimeout(twoNote, 5000 + Math.random() * 8000));
        };
        
        // Bird type 3: Trilling song
        const trill = () => {
            if (!this.activeSounds.has('Forest')) return;
            
            const baseFreq = 3000 + Math.random() * 1000;
            const now = this.audioContext.currentTime;
            const duration = 0.6 + Math.random() * 0.4;
            
            const osc = this.audioContext.createOscillator();
            osc.type = 'sine';
            
            // Rapid frequency changes for trill
            for (let t = 0; t < duration; t += 0.04) {
                osc.frequency.setValueAtTime(
                    baseFreq + Math.sin(t * 50) * 400,
                    now + t
                );
            }
            
            const trillGain = this.audioContext.createGain();
            trillGain.gain.setValueAtTime(0, now);
            trillGain.gain.linearRampToValueAtTime(0.12, now + 0.05);
            trillGain.gain.setValueAtTime(0.12, now + duration - 0.1);
            trillGain.gain.exponentialRampToValueAtTime(0.001, now + duration);
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.5;
            
            osc.connect(trillGain);
            trillGain.connect(pan);
            pan.connect(gainNode);
            
            osc.start(now);
            osc.stop(now + duration + 0.05);
            
            birdTimeouts.push(setTimeout(trill, 6000 + Math.random() * 10000));
        };
        
        // Bird type 4: Woodpecker
        const woodpecker = () => {
            if (!this.activeSounds.has('Forest')) return;
            
            const taps = 4 + Math.floor(Math.random() * 5);
            const now = this.audioContext.currentTime;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.2;
            pan.connect(gainNode);
            
            for (let i = 0; i < taps; i++) {
                const osc = this.audioContext.createOscillator();
                osc.frequency.value = 1500 + Math.random() * 500;
                osc.type = 'sine';
                
                const tapGain = this.audioContext.createGain();
                const tapTime = now + i * 0.08;
                tapGain.gain.setValueAtTime(0.2, tapTime);
                tapGain.gain.exponentialRampToValueAtTime(0.001, tapTime + 0.02);
                
                osc.connect(tapGain);
                tapGain.connect(pan);
                osc.start(tapTime);
                osc.stop(tapTime + 0.03);
            }
            
            birdTimeouts.push(setTimeout(woodpecker, 12000 + Math.random() * 15000));
        };
        
        // Start birds
        birdTimeouts.push(setTimeout(chirp, 500));
        birdTimeouts.push(setTimeout(twoNote, 2000));
        birdTimeouts.push(setTimeout(trill, 4000));
        birdTimeouts.push(setTimeout(woodpecker, 8000));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => birdTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ THUNDER - Clean thunder claps ============
    createThunder(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.7;
        gainNode.connect(this.masterGain);
        
        let thunderTimeouts = [];
        
        const strike = () => {
            if (!this.activeSounds.has('Thunder')) return;
            
            const sampleRate = this.audioContext.sampleRate;
            const now = this.audioContext.currentTime;
            
            // Initial crack
            const crackDuration = 0.1;
            const crackBuffer = this.audioContext.createBuffer(2, crackDuration * sampleRate, sampleRate);
            for (let ch = 0; ch < 2; ch++) {
                const data = crackBuffer.getChannelData(ch);
                for (let i = 0; i < data.length; i++) {
                    const env = Math.exp(-i / (sampleRate * 0.02));
                    data[i] = (Math.random() * 2 - 1) * env;
                }
            }
            
            const crackSource = this.audioContext.createBufferSource();
            crackSource.buffer = crackBuffer;
            
            const crackFilter = this.audioContext.createBiquadFilter();
            crackFilter.type = 'highpass';
            crackFilter.frequency.value = 500;
            
            const crackGain = this.audioContext.createGain();
            crackGain.gain.value = 0.6;
            
            crackSource.connect(crackFilter);
            crackFilter.connect(crackGain);
            crackGain.connect(gainNode);
            crackSource.start(now);
            
            // Rolling thunder
            const rollDuration = 2 + Math.random() * 2;
            const rollBuffer = this.audioContext.createBuffer(2, rollDuration * sampleRate, sampleRate);
            for (let ch = 0; ch < 2; ch++) {
                const data = rollBuffer.getChannelData(ch);
                let last = 0;
                for (let i = 0; i < data.length; i++) {
                    const t = i / sampleRate;
                    const env = Math.exp(-t * 1.5);
                    const white = Math.random() * 2 - 1;
                    last = (last + 0.02 * white) / 1.02;
                    data[i] = last * env * 3;
                }
            }
            
            const rollSource = this.audioContext.createBufferSource();
            rollSource.buffer = rollBuffer;
            
            const rollFilter = this.audioContext.createBiquadFilter();
            rollFilter.type = 'lowpass';
            rollFilter.frequency.value = 150;
            
            const rollGain = this.audioContext.createGain();
            rollGain.gain.value = 0.5;
            
            rollSource.connect(rollFilter);
            rollFilter.connect(rollGain);
            rollGain.connect(gainNode);
            rollSource.start(now + 0.05);
            
            thunderTimeouts.push(setTimeout(strike, 8000 + Math.random() * 15000));
        };
        
        thunderTimeouts.push(setTimeout(strike, 2000));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => thunderTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ WIND - Clean gentle wind ============
    createWind(volume = 0.5) {
        const sampleRate = this.audioContext.sampleRate;
        const duration = 8;
        const buffer = this.audioContext.createBuffer(2, duration * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = buffer.getChannelData(ch);
            let last = 0;
            for (let i = 0; i < data.length; i++) {
                const t = i / sampleRate;
                const white = Math.random() * 2 - 1;
                // Smooth variation
                const mod = Math.sin(t * 0.3) * 0.3 + Math.sin(t * 0.13) * 0.2 + 0.5;
                last = (last + 0.01 * white) / 1.01;
                data[i] = last * mod * 2;
            }
        }
        
        const source = this.audioContext.createBufferSource();
        source.buffer = buffer;
        source.loop = true;
        
        const filter = this.audioContext.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.value = 500;
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.4;
        
        source.connect(filter);
        filter.connect(gainNode);
        gainNode.connect(this.masterGain);
        source.start();
        
        return { source, gain: gainNode, type: 'generated' };
    }

    // ============ WATERFALL - Clean rushing water ============
    createWaterfall(volume = 0.5) {
        const sampleRate = this.audioContext.sampleRate;
        const duration = 4;
        const buffer = this.audioContext.createBuffer(2, duration * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = buffer.getChannelData(ch);
            for (let i = 0; i < data.length; i++) {
                data[i] = Math.random() * 2 - 1;
            }
        }
        
        const source = this.audioContext.createBufferSource();
        source.buffer = buffer;
        source.loop = true;
        
        const filter = this.audioContext.createBiquadFilter();
        filter.type = 'bandpass';
        filter.frequency.value = 1200;
        filter.Q.value = 0.5;
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.35;
        
        source.connect(filter);
        filter.connect(gainNode);
        gainNode.connect(this.masterGain);
        source.start();
        
        return { source, gain: gainNode, type: 'generated' };
    }

    // ============ FIREPLACE - Professional Crackling Fire ============
    // Real fire sound = filtered noise (base roar) + occasional crackles/pops
    createFireplace(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.6;
        gainNode.connect(this.masterGain);
        
        const sampleRate = this.audioContext.sampleRate;
        let crackleTimeouts = [];
        
        // =====================================================
        // LAYER 1: FIRE BASE - Low rumbling roar
        // The continuous "whoosh" of the fire
        // =====================================================
        
        const fireBaseDuration = 10;
        const fireBaseBuffer = this.audioContext.createBuffer(2, fireBaseDuration * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = fireBaseBuffer.getChannelData(ch);
            let lastValue = 0;
            
            for (let i = 0; i < data.length; i++) {
                const t = i / sampleRate;
                const white = Math.random() * 2 - 1;
                
                // Brown noise for low rumble
                lastValue = (lastValue + 0.02 * white) / 1.02;
                
                // Add slow undulation (fire breathing)
                const breathe = 0.7 + 0.3 * Math.sin(t * 0.8) * Math.sin(t * 0.5);
                data[i] = lastValue * breathe * 4;
            }
            
            // Smooth it
            for (let pass = 0; pass < 3; pass++) {
                for (let i = 1; i < data.length - 1; i++) {
                    data[i] = (data[i-1] + data[i] + data[i+1]) / 3;
                }
            }
        }
        
        const fireBaseSource = this.audioContext.createBufferSource();
        fireBaseSource.buffer = fireBaseBuffer;
        fireBaseSource.loop = true;
        
        const fireBaseLowpass = this.audioContext.createBiquadFilter();
        fireBaseLowpass.type = 'lowpass';
        fireBaseLowpass.frequency.value = 400;
        
        const fireBaseGain = this.audioContext.createGain();
        fireBaseGain.gain.value = 0.35;
        
        fireBaseSource.connect(fireBaseLowpass);
        fireBaseLowpass.connect(fireBaseGain);
        fireBaseGain.connect(gainNode);
        fireBaseSource.start();
        
        // =====================================================
        // LAYER 2: FIRE TEXTURE - Mid frequency hiss/sizzle
        // The "ssshhh" of burning wood
        // =====================================================
        
        const fireTextureDuration = 8;
        const fireTextureBuffer = this.audioContext.createBuffer(2, fireTextureDuration * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = fireTextureBuffer.getChannelData(ch);
            let b0 = 0, b1 = 0, b2 = 0;
            
            for (let i = 0; i < data.length; i++) {
                const t = i / sampleRate;
                const white = Math.random() * 2 - 1;
                
                // Pink noise
                b0 = 0.99765 * b0 + white * 0.0990460;
                b1 = 0.96300 * b1 + white * 0.2965164;
                b2 = 0.57000 * b2 + white * 1.0526913;
                const pink = (b0 + b1 + b2 + white * 0.1848) * 0.11;
                
                // Gentle modulation
                const mod = 0.8 + 0.2 * Math.sin(t * 1.2) * Math.sin(t * 0.7);
                data[i] = pink * mod;
            }
        }
        
        const fireTextureSource = this.audioContext.createBufferSource();
        fireTextureSource.buffer = fireTextureBuffer;
        fireTextureSource.loop = true;
        
        const fireTextureBandpass = this.audioContext.createBiquadFilter();
        fireTextureBandpass.type = 'bandpass';
        fireTextureBandpass.frequency.value = 2000;
        fireTextureBandpass.Q.value = 0.5;
        
        const fireTextureGain = this.audioContext.createGain();
        fireTextureGain.gain.value = 0.15;
        
        fireTextureSource.connect(fireTextureBandpass);
        fireTextureBandpass.connect(fireTextureGain);
        fireTextureGain.connect(gainNode);
        fireTextureSource.start();
        
        // =====================================================
        // LAYER 3: CRACKLES - Noise bursts, not tones
        // Real crackles are filtered noise impulses
        // =====================================================
        
        const createCrackle = () => {
            if (!this.activeSounds.has('Fireplace')) return;
            
            const now = this.audioContext.currentTime;
            
            // Create a short noise burst
            const crackleLength = 0.02 + Math.random() * 0.03;
            const crackleBuffer = this.audioContext.createBuffer(1, crackleLength * sampleRate, sampleRate);
            const data = crackleBuffer.getChannelData(0);
            
            for (let i = 0; i < data.length; i++) {
                const t = i / data.length;
                // Fast attack, quick decay
                const env = Math.exp(-t * 15) * (1 - Math.exp(-t * 100));
                data[i] = (Math.random() * 2 - 1) * env;
            }
            
            const crackleSource = this.audioContext.createBufferSource();
            crackleSource.buffer = crackleBuffer;
            
            // Bandpass to shape the crackle
            const crackleFilter = this.audioContext.createBiquadFilter();
            crackleFilter.type = 'bandpass';
            crackleFilter.frequency.value = 2000 + Math.random() * 3000;
            crackleFilter.Q.value = 1 + Math.random() * 2;
            
            const crackleGain = this.audioContext.createGain();
            crackleGain.gain.value = 0.15 + Math.random() * 0.15;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.2;
            
            crackleSource.connect(crackleFilter);
            crackleFilter.connect(crackleGain);
            crackleGain.connect(pan);
            pan.connect(gainNode);
            
            crackleSource.start(now);
            
            // Schedule next crackle (variable timing for natural feel)
            const nextCrackle = 50 + Math.random() * 200;
            crackleTimeouts.push(setTimeout(createCrackle, nextCrackle));
        };
        
        // =====================================================
        // LAYER 4: POPS - Deeper, longer wood pops
        // =====================================================
        
        const createPop = () => {
            if (!this.activeSounds.has('Fireplace')) return;
            
            const now = this.audioContext.currentTime;
            
            // Longer noise burst for pops
            const popLength = 0.05 + Math.random() * 0.05;
            const popBuffer = this.audioContext.createBuffer(1, popLength * sampleRate, sampleRate);
            const data = popBuffer.getChannelData(0);
            
            for (let i = 0; i < data.length; i++) {
                const t = i / data.length;
                // Pop envelope - quick attack, medium decay
                const env = Math.exp(-t * 8) * (1 - Math.exp(-t * 50));
                data[i] = (Math.random() * 2 - 1) * env;
            }
            
            const popSource = this.audioContext.createBufferSource();
            popSource.buffer = popBuffer;
            
            // Lower frequency for pops
            const popFilter = this.audioContext.createBiquadFilter();
            popFilter.type = 'lowpass';
            popFilter.frequency.value = 800 + Math.random() * 600;
            
            const popGain = this.audioContext.createGain();
            popGain.gain.value = 0.25 + Math.random() * 0.15;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 0.8;
            
            popSource.connect(popFilter);
            popFilter.connect(popGain);
            popGain.connect(pan);
            pan.connect(gainNode);
            
            popSource.start(now);
            
            // Pops are less frequent
            const nextPop = 300 + Math.random() * 700;
            crackleTimeouts.push(setTimeout(createPop, nextPop));
        };
        
        // =====================================================
        // LAYER 5: SIZZLES - High frequency ember sounds
        // =====================================================
        
        const createSizzle = () => {
            if (!this.activeSounds.has('Fireplace')) return;
            
            const now = this.audioContext.currentTime;
            
            const sizzleLength = 0.1 + Math.random() * 0.2;
            const sizzleBuffer = this.audioContext.createBuffer(1, sizzleLength * sampleRate, sampleRate);
            const data = sizzleBuffer.getChannelData(0);
            
            for (let i = 0; i < data.length; i++) {
                const t = i / data.length;
                // Sizzle - gradual attack, slow decay
                const env = Math.sin(t * Math.PI) * Math.exp(-t * 3);
                data[i] = (Math.random() * 2 - 1) * env * 0.3;
            }
            
            const sizzleSource = this.audioContext.createBufferSource();
            sizzleSource.buffer = sizzleBuffer;
            
            const sizzleFilter = this.audioContext.createBiquadFilter();
            sizzleFilter.type = 'highpass';
            sizzleFilter.frequency.value = 4000 + Math.random() * 2000;
            
            const sizzleGain = this.audioContext.createGain();
            sizzleGain.gain.value = 0.08;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.4;
            
            sizzleSource.connect(sizzleFilter);
            sizzleFilter.connect(sizzleGain);
            sizzleGain.connect(pan);
            pan.connect(gainNode);
            
            sizzleSource.start(now);
            
            const nextSizzle = 200 + Math.random() * 500;
            crackleTimeouts.push(setTimeout(createSizzle, nextSizzle));
        };
        
        // Start the crackle/pop/sizzle layers
        crackleTimeouts.push(setTimeout(createCrackle, 100));
        crackleTimeouts.push(setTimeout(createCrackle, 200));
        crackleTimeouts.push(setTimeout(createPop, 500));
        crackleTimeouts.push(setTimeout(createSizzle, 300));
        
        return {
            source: [fireBaseSource, fireTextureSource],
            gain: gainNode,
            type: 'generated',
            cleanup: () => crackleTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ COFFEE SHOP - Clean cafe ambiance ============
    createCoffeeShop(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.4;
        gainNode.connect(this.masterGain);
        
        let cafeTimeouts = [];
        
        // Soft murmur base
        const sampleRate = this.audioContext.sampleRate;
        const buffer = this.audioContext.createBuffer(2, 3 * sampleRate, sampleRate);
        for (let ch = 0; ch < 2; ch++) {
            const data = buffer.getChannelData(ch);
            for (let i = 0; i < data.length; i++) {
                data[i] = (Math.random() * 2 - 1) * 0.1;
            }
        }
        
        const murmurSource = this.audioContext.createBufferSource();
        murmurSource.buffer = buffer;
        murmurSource.loop = true;
        
        const murmurFilter = this.audioContext.createBiquadFilter();
        murmurFilter.type = 'bandpass';
        murmurFilter.frequency.value = 600;
        murmurFilter.Q.value = 0.8;
        
        murmurSource.connect(murmurFilter);
        murmurFilter.connect(gainNode);
        murmurSource.start();
        
        // Occasional cup clink
        const clink = () => {
            if (!this.activeSounds.has('Coffee Shop')) return;
            
            const freq = 2000 + Math.random() * 1500;
            const now = this.audioContext.currentTime;
            
            const osc = this.audioContext.createOscillator();
            osc.frequency.value = freq;
            osc.type = 'sine';
            
            const clinkGain = this.audioContext.createGain();
            clinkGain.gain.setValueAtTime(0.1, now);
            clinkGain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.5;
            
            osc.connect(clinkGain);
            clinkGain.connect(pan);
            pan.connect(gainNode);
            osc.start(now);
            osc.stop(now + 0.35);
            
            cafeTimeouts.push(setTimeout(clink, 5000 + Math.random() * 10000));
        };
        
        cafeTimeouts.push(setTimeout(clink, 3000));
        
        return {
            source: murmurSource,
            gain: gainNode,
            type: 'generated',
            cleanup: () => cafeTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ WHITE NOISE ============
    createWhiteNoise(volume = 0.5) {
        const sampleRate = this.audioContext.sampleRate;
        const buffer = this.audioContext.createBuffer(2, 2 * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = buffer.getChannelData(ch);
            for (let i = 0; i < data.length; i++) {
                data[i] = Math.random() * 2 - 1;
            }
        }
        
        const source = this.audioContext.createBufferSource();
        source.buffer = buffer;
        source.loop = true;
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.2;
        
        source.connect(gainNode);
        gainNode.connect(this.masterGain);
        source.start();
        
        return { source, gain: gainNode, type: 'generated' };
    }

    // ============ PINK NOISE ============
    createPinkNoise(volume = 0.5) {
        const sampleRate = this.audioContext.sampleRate;
        const buffer = this.audioContext.createBuffer(2, 2 * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = buffer.getChannelData(ch);
            let b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
            for (let i = 0; i < data.length; i++) {
                const white = Math.random() * 2 - 1;
                b0 = 0.99886 * b0 + white * 0.0555179;
                b1 = 0.99332 * b1 + white * 0.0750759;
                b2 = 0.96900 * b2 + white * 0.1538520;
                b3 = 0.86650 * b3 + white * 0.3104856;
                b4 = 0.55000 * b4 + white * 0.5329522;
                b5 = -0.7616 * b5 - white * 0.0168980;
                data[i] = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362) * 0.11;
                b6 = white * 0.115926;
            }
        }
        
        const source = this.audioContext.createBufferSource();
        source.buffer = buffer;
        source.loop = true;
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.3;
        
        source.connect(gainNode);
        gainNode.connect(this.masterGain);
        source.start();
        
        return { source, gain: gainNode, type: 'generated' };
    }

    // ============ BROWN NOISE ============
    createBrownNoise(volume = 0.5) {
        const sampleRate = this.audioContext.sampleRate;
        const buffer = this.audioContext.createBuffer(2, 2 * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = buffer.getChannelData(ch);
            let last = 0;
            for (let i = 0; i < data.length; i++) {
                const white = Math.random() * 2 - 1;
                data[i] = (last + 0.02 * white) / 1.02;
                last = data[i];
                data[i] *= 3.5;
            }
        }
        
        const source = this.audioContext.createBufferSource();
        source.buffer = buffer;
        source.loop = true;
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.35;
        
        source.connect(gainNode);
        gainNode.connect(this.masterGain);
        source.start();
        
        return { source, gain: gainNode, type: 'generated' };
    }

    // ============ BINAURAL BEATS ============
    createBinauralBeats(volume = 0.5) {
        const baseFreq = 200;
        const beatFreq = 10; // Alpha waves
        
        const oscL = this.audioContext.createOscillator();
        oscL.frequency.value = baseFreq;
        oscL.type = 'sine';
        
        const oscR = this.audioContext.createOscillator();
        oscR.frequency.value = baseFreq + beatFreq;
        oscR.type = 'sine';
        
        const panL = this.audioContext.createStereoPanner();
        panL.pan.value = -1;
        
        const panR = this.audioContext.createStereoPanner();
        panR.pan.value = 1;
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.15;
        
        oscL.connect(panL);
        oscR.connect(panR);
        panL.connect(gainNode);
        panR.connect(gainNode);
        gainNode.connect(this.masterGain);
        
        oscL.start();
        oscR.start();
        
        return { source: [oscL, oscR], gain: gainNode, type: 'generated' };
    }

    // ============ SINGING BOWLS ============
    createSingingBowl(volume = 0.5) {
        const freqs = [174, 285, 396, 417, 528, 639];
        const oscillators = [];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.12;
        gainNode.connect(this.masterGain);
        
        freqs.forEach((freq, i) => {
            const osc = this.audioContext.createOscillator();
            osc.frequency.value = freq;
            osc.type = 'sine';
            
            // Subtle vibrato
            const lfo = this.audioContext.createOscillator();
            lfo.frequency.value = 0.3 + Math.random() * 0.3;
            const lfoGain = this.audioContext.createGain();
            lfoGain.gain.value = 1.5;
            lfo.connect(lfoGain);
            lfoGain.connect(osc.frequency);
            
            const oscGain = this.audioContext.createGain();
            oscGain.gain.value = 0.1 / (i + 1);
            
            osc.connect(oscGain);
            oscGain.connect(gainNode);
            osc.start();
            lfo.start();
            oscillators.push(osc, lfo);
        });
        
        return { source: oscillators, gain: gainNode, type: 'generated' };
    }

    // ============ CHIMES ============
    createChimes(volume = 0.5) {
        const notes = [523, 587, 659, 698, 784, 880, 988];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.15;
        gainNode.connect(this.masterGain);
        
        let chimeTimeouts = [];
        
        const playChime = () => {
            if (!this.activeSounds.has('Chimes')) return;
            
            const freq = notes[Math.floor(Math.random() * notes.length)];
            const now = this.audioContext.currentTime;
            
            const osc = this.audioContext.createOscillator();
            osc.frequency.value = freq;
            osc.type = 'sine';
            
            // Harmonic for metallic sound
            const osc2 = this.audioContext.createOscillator();
            osc2.frequency.value = freq * 2.76;
            osc2.type = 'sine';
            
            const chimeGain = this.audioContext.createGain();
            chimeGain.gain.setValueAtTime(0.25, now);
            chimeGain.gain.exponentialRampToValueAtTime(0.001, now + 4);
            
            const harmGain = this.audioContext.createGain();
            harmGain.gain.setValueAtTime(0.06, now);
            harmGain.gain.exponentialRampToValueAtTime(0.001, now + 2);
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = (Math.random() - 0.5) * 1.4;
            
            osc.connect(chimeGain);
            osc2.connect(harmGain);
            chimeGain.connect(pan);
            harmGain.connect(pan);
            pan.connect(gainNode);
            
            osc.start(now);
            osc2.start(now);
            osc.stop(now + 4);
            osc2.stop(now + 2);
            
            chimeTimeouts.push(setTimeout(playChime, 2000 + Math.random() * 5000));
        };
        
        chimeTimeouts.push(setTimeout(playChime, 500));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => chimeTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ TEMPLE BELLS ============
    createTempleBells(volume = 0.5) {
        const bellNotes = [174, 220, 262];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.2;
        gainNode.connect(this.masterGain);
        
        let bellTimeouts = [];
        
        const playBell = () => {
            if (!this.activeSounds.has('Temple Bells')) return;
            
            const freq = bellNotes[Math.floor(Math.random() * bellNotes.length)];
            const now = this.audioContext.currentTime;
            
            const osc = this.audioContext.createOscillator();
            osc.frequency.value = freq;
            osc.type = 'sine';
            
            const bellGain = this.audioContext.createGain();
            bellGain.gain.setValueAtTime(0.35, now);
            bellGain.gain.exponentialRampToValueAtTime(0.001, now + 8);
            
            osc.connect(bellGain);
            bellGain.connect(gainNode);
            osc.start(now);
            osc.stop(now + 8);
            
            bellTimeouts.push(setTimeout(playBell, 8000 + Math.random() * 12000));
        };
        
        bellTimeouts.push(setTimeout(playBell, 1000));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => bellTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ OM CHANT ============
    createOmChant(volume = 0.5) {
        const baseFreq = 136.1;
        const oscillators = [];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.15;
        gainNode.connect(this.masterGain);
        
        // Breathing LFO
        const lfo = this.audioContext.createOscillator();
        lfo.frequency.value = 0.08;
        const lfoGain = this.audioContext.createGain();
        lfoGain.gain.value = 0.1;
        lfo.connect(lfoGain);
        lfo.start();
        oscillators.push(lfo);
        
        // Fundamental and harmonics
        [1, 2, 3, 4].forEach((h, i) => {
            const osc = this.audioContext.createOscillator();
            osc.frequency.value = baseFreq * h;
            osc.type = 'sine';
            
            const oscGain = this.audioContext.createGain();
            oscGain.gain.value = 0.2 / (i + 1);
            lfoGain.connect(oscGain.gain);
            
            osc.connect(oscGain);
            oscGain.connect(gainNode);
            osc.start();
            oscillators.push(osc);
        });
        
        return { source: oscillators, gain: gainNode, type: 'generated' };
    }

    // ============ AMBIENT PADS - Pro Quality Soulful Ambience ============
    createAmbientPad(volume = 0.5) {
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.25;
        gainNode.connect(this.masterGain);
        
        const sampleRate = this.audioContext.sampleRate;
        let oscillators = [];
        let timeouts = [];
        
        // =====================================================
        // LUSH REVERB - Creates the ambient space
        // =====================================================
        
        const createLushReverb = () => {
            const convolver = this.audioContext.createConvolver();
            const reverbTime = 4; // Long, lush reverb
            const reverbBuffer = this.audioContext.createBuffer(2, reverbTime * sampleRate, sampleRate);
            
            for (let ch = 0; ch < 2; ch++) {
                const data = reverbBuffer.getChannelData(ch);
                for (let i = 0; i < data.length; i++) {
                    // Exponential decay with diffusion
                    const decay = Math.exp(-2 * i / data.length);
                    // Early reflections + late diffuse tail
                    const early = i < sampleRate * 0.1 ? Math.random() * 0.3 : 0;
                    const late = Math.random() * 2 - 1;
                    data[i] = (early + late) * decay;
                }
            }
            convolver.buffer = reverbBuffer;
            return convolver;
        };
        
        const reverb = createLushReverb();
        const reverbGain = this.audioContext.createGain();
        reverbGain.gain.value = 0.5; // 50% wet
        reverb.connect(reverbGain);
        reverbGain.connect(gainNode);
        
        const dryGain = this.audioContext.createGain();
        dryGain.gain.value = 0.5;
        dryGain.connect(gainNode);
        
        // Main filter for warmth
        const mainFilter = this.audioContext.createBiquadFilter();
        mainFilter.type = 'lowpass';
        mainFilter.frequency.value = 2500;
        mainFilter.Q.value = 0.5;
        mainFilter.connect(dryGain);
        mainFilter.connect(reverb);
        
        // =====================================================
        // CHORD VOICINGS - Beautiful, emotional progressions
        // Using jazz-inspired voicings for depth
        // =====================================================
        
        const chordProgressions = [
            // Cmaj9 - Ethereal, open
            { notes: [130.81, 196.00, 246.94, 293.66, 369.99], name: 'Cmaj9' },
            // Am11 - Deep, soulful
            { notes: [110.00, 164.81, 220.00, 293.66, 329.63], name: 'Am11' },
            // Fmaj7#11 - Dreamy, floating
            { notes: [87.31, 130.81, 174.61, 246.94, 329.63], name: 'Fmaj7#11' },
            // Dm9 - Melancholic beauty
            { notes: [73.42, 146.83, 220.00, 277.18, 329.63], name: 'Dm9' },
            // G13 - Hopeful anticipation
            { notes: [98.00, 146.83, 196.00, 246.94, 349.23], name: 'G13' },
            // Em9 - Introspective
            { notes: [82.41, 123.47, 164.81, 246.94, 293.66], name: 'Em9' },
            // Bbmaj7 - Warm surprise
            { notes: [116.54, 146.83, 174.61, 233.08, 293.66], name: 'Bbmaj7' },
            // Cmaj7 - Resolution, peace
            { notes: [130.81, 164.81, 196.00, 246.94, 329.63], name: 'Cmaj7' }
        ];
        
        let currentChord = 0;
        let activeOscillators = [];
        
        // =====================================================
        // PAD VOICE - Warm, evolving texture
        // =====================================================
        
        const createPadVoice = (freq, volume, pan = 0) => {
            // Main oscillator - warm sawtooth filtered heavily
            const osc1 = this.audioContext.createOscillator();
            osc1.type = 'sawtooth';
            osc1.frequency.value = freq;
            
            // Detuned oscillator for width
            const osc2 = this.audioContext.createOscillator();
            osc2.type = 'sawtooth';
            osc2.frequency.value = freq;
            osc2.detune.value = 7; // Slight detune
            
            // Sub oscillator (one octave down)
            const oscSub = this.audioContext.createOscillator();
            oscSub.type = 'sine';
            oscSub.frequency.value = freq / 2;
            
            // Triangle for smoothness
            const osc3 = this.audioContext.createOscillator();
            osc3.type = 'triangle';
            osc3.frequency.value = freq;
            osc3.detune.value = -5;
            
            // Individual voice filter (slowly modulated)
            const voiceFilter = this.audioContext.createBiquadFilter();
            voiceFilter.type = 'lowpass';
            voiceFilter.frequency.value = 800 + Math.random() * 400;
            voiceFilter.Q.value = 2;
            
            // Voice gain
            const voiceGain = this.audioContext.createGain();
            voiceGain.gain.value = 0;
            
            // Stereo positioning
            const panner = this.audioContext.createStereoPanner();
            panner.pan.value = pan;
            
            // Mix oscillators
            const osc1Gain = this.audioContext.createGain();
            osc1Gain.gain.value = 0.3;
            const osc2Gain = this.audioContext.createGain();
            osc2Gain.gain.value = 0.25;
            const osc3Gain = this.audioContext.createGain();
            osc3Gain.gain.value = 0.3;
            const subGain = this.audioContext.createGain();
            subGain.gain.value = 0.15;
            
            osc1.connect(osc1Gain);
            osc2.connect(osc2Gain);
            osc3.connect(osc3Gain);
            oscSub.connect(subGain);
            
            osc1Gain.connect(voiceFilter);
            osc2Gain.connect(voiceFilter);
            osc3Gain.connect(voiceFilter);
            subGain.connect(voiceFilter);
            
            voiceFilter.connect(voiceGain);
            voiceGain.connect(panner);
            panner.connect(mainFilter);
            
            return {
                oscillators: [osc1, osc2, osc3, oscSub],
                gain: voiceGain,
                filter: voiceFilter,
                start: () => {
                    osc1.start();
                    osc2.start();
                    osc3.start();
                    oscSub.start();
                },
                stop: (time) => {
                    osc1.stop(time);
                    osc2.stop(time);
                    osc3.stop(time);
                    oscSub.stop(time);
                }
            };
        };
        
        // =====================================================
        // PLAY CHORD - Smooth crossfade between chords
        // =====================================================
        
        const playChord = () => {
            if (!this.activeSounds.has('Ambient Pads')) return;
            
            const chord = chordProgressions[currentChord];
            const now = this.audioContext.currentTime;
            
            // Fade out previous chord smoothly
            activeOscillators.forEach((voice, i) => {
                voice.gain.gain.linearRampToValueAtTime(0, now + 3);
                voice.stop(now + 3.5);
            });
            
            // Create new voices
            const newVoices = [];
            chord.notes.forEach((freq, i) => {
                // Spread notes across stereo field
                const pan = (i / (chord.notes.length - 1)) * 1.2 - 0.6;
                const vol = 0.12 - (i * 0.015); // Lower notes slightly louder
                
                const voice = createPadVoice(freq, vol, pan);
                voice.start();
                
                // Slow fade in (swell)
                voice.gain.gain.setValueAtTime(0, now);
                voice.gain.gain.linearRampToValueAtTime(vol, now + 4);
                
                // Subtle filter movement for life
                const filterStart = 600 + Math.random() * 400;
                const filterEnd = 1000 + Math.random() * 600;
                voice.filter.frequency.setValueAtTime(filterStart, now);
                voice.filter.frequency.linearRampToValueAtTime(filterEnd, now + 6);
                
                newVoices.push(voice);
                voice.oscillators.forEach(o => oscillators.push(o));
            });
            
            activeOscillators = newVoices;
            currentChord = (currentChord + 1) % chordProgressions.length;
            
            // Next chord in 8-12 seconds
            timeouts.push(setTimeout(playChord, 8000 + Math.random() * 4000));
        };
        
        // =====================================================
        // SHIMMER LAYER - High ethereal texture
        // =====================================================
        
        const createShimmer = () => {
            if (!this.activeSounds.has('Ambient Pads')) return;
            
            const now = this.audioContext.currentTime;
            const chord = chordProgressions[currentChord];
            
            // Pick a random note from current chord, play 2 octaves up
            const baseFreq = chord.notes[Math.floor(Math.random() * chord.notes.length)];
            const shimmerFreq = baseFreq * 4; // 2 octaves up
            
            const osc = this.audioContext.createOscillator();
            osc.type = 'sine';
            osc.frequency.value = shimmerFreq;
            
            const shimmerGain = this.audioContext.createGain();
            shimmerGain.gain.setValueAtTime(0, now);
            shimmerGain.gain.linearRampToValueAtTime(0.03, now + 1);
            shimmerGain.gain.linearRampToValueAtTime(0, now + 4);
            
            const shimmerFilter = this.audioContext.createBiquadFilter();
            shimmerFilter.type = 'bandpass';
            shimmerFilter.frequency.value = shimmerFreq;
            shimmerFilter.Q.value = 5;
            
            const pan = this.audioContext.createStereoPanner();
            pan.pan.value = Math.random() * 1.4 - 0.7;
            
            osc.connect(shimmerFilter);
            shimmerFilter.connect(shimmerGain);
            shimmerGain.connect(pan);
            pan.connect(reverb);
            
            osc.start(now);
            osc.stop(now + 5);
            oscillators.push(osc);
            
            timeouts.push(setTimeout(createShimmer, 3000 + Math.random() * 4000));
        };
        
        // =====================================================
        // SUB BASS PULSE - Deep, subtle movement
        // =====================================================
        
        const createSubPulse = () => {
            if (!this.activeSounds.has('Ambient Pads')) return;
            
            const now = this.audioContext.currentTime;
            const chord = chordProgressions[currentChord];
            const rootFreq = chord.notes[0] / 2; // One octave below root
            
            const osc = this.audioContext.createOscillator();
            osc.type = 'sine';
            osc.frequency.value = rootFreq;
            
            const subGain = this.audioContext.createGain();
            subGain.gain.setValueAtTime(0, now);
            subGain.gain.linearRampToValueAtTime(0.08, now + 2);
            subGain.gain.linearRampToValueAtTime(0, now + 6);
            
            const subFilter = this.audioContext.createBiquadFilter();
            subFilter.type = 'lowpass';
            subFilter.frequency.value = 150;
            
            osc.connect(subFilter);
            subFilter.connect(subGain);
            subGain.connect(dryGain);
            
            osc.start(now);
            osc.stop(now + 7);
            oscillators.push(osc);
            
            timeouts.push(setTimeout(createSubPulse, 6000 + Math.random() * 4000));
        };
        
        // =====================================================
        // BREATH - Slow filtered noise for organic feel
        // =====================================================
        
        const breathDuration = 15;
        const breathBuffer = this.audioContext.createBuffer(2, breathDuration * sampleRate, sampleRate);
        
        for (let ch = 0; ch < 2; ch++) {
            const data = breathBuffer.getChannelData(ch);
            let b0 = 0, b1 = 0, b2 = 0;
            
            for (let i = 0; i < data.length; i++) {
                const white = Math.random() * 2 - 1;
                b0 = 0.99765 * b0 + white * 0.0990460;
                b1 = 0.96300 * b1 + white * 0.2965164;
                b2 = 0.57000 * b2 + white * 1.0526913;
                const pink = (b0 + b1 + b2 + white * 0.1848) * 0.06;
                
                // Very slow modulation
                const t = i / sampleRate;
                const mod = 0.5 + 0.5 * Math.sin(t * 0.15) * Math.sin(t * 0.09);
                data[i] = pink * mod;
            }
        }
        
        const breathSource = this.audioContext.createBufferSource();
        breathSource.buffer = breathBuffer;
        breathSource.loop = true;
        
        const breathFilter = this.audioContext.createBiquadFilter();
        breathFilter.type = 'bandpass';
        breathFilter.frequency.value = 400;
        breathFilter.Q.value = 0.3;
        
        const breathGain = this.audioContext.createGain();
        breathGain.gain.value = 0.04;
        
        breathSource.connect(breathFilter);
        breathFilter.connect(breathGain);
        breathGain.connect(reverb);
        breathSource.start();
        oscillators.push(breathSource);
        
        // =====================================================
        // START EVERYTHING
        // =====================================================
        
        // Start first chord immediately
        playChord();
        
        // Start shimmer and sub pulse with delays
        timeouts.push(setTimeout(createShimmer, 2000));
        timeouts.push(setTimeout(createSubPulse, 4000));
        
        return { 
            source: oscillators, 
            gain: gainNode, 
            type: 'generated',
            cleanup: () => timeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ PIANO - Beautiful chord progressions ============
    createPiano(volume = 0.5) {
        // Beautiful chord progressions (C major emotional progression)
        const chordProgressions = [
            // C major (peaceful)
            [261.63, 329.63, 392.00],
            // Am (melancholic)
            [220.00, 261.63, 329.63],
            // F major (warm)
            [174.61, 261.63, 349.23],
            // G major (hopeful)
            [196.00, 246.94, 392.00],
            // Em (introspective)
            [164.81, 246.94, 329.63],
            // Dm (tender)
            [146.83, 220.00, 293.66],
            // G7 (anticipation)
            [196.00, 246.94, 349.23],
            // C major add9 (resolution)
            [261.63, 329.63, 392.00, 587.33]
        ];
        
        // Individual melodic notes for embellishment
        const melodyNotes = [523.25, 587.33, 659.25, 698.46, 783.99, 880.00, 987.77, 1046.50];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.18;
        gainNode.connect(this.masterGain);
        
        // Add reverb effect using convolver
        const convolver = this.audioContext.createConvolver();
        const reverbTime = 2;
        const sampleRate = this.audioContext.sampleRate;
        const reverbBuffer = this.audioContext.createBuffer(2, reverbTime * sampleRate, sampleRate);
        for (let ch = 0; ch < 2; ch++) {
            const data = reverbBuffer.getChannelData(ch);
            for (let i = 0; i < data.length; i++) {
                data[i] = (Math.random() * 2 - 1) * Math.exp(-3 * i / data.length);
            }
        }
        convolver.buffer = reverbBuffer;
        
        const dryGain = this.audioContext.createGain();
        dryGain.gain.value = 0.7;
        dryGain.connect(gainNode);
        
        const wetGain = this.audioContext.createGain();
        wetGain.gain.value = 0.3;
        convolver.connect(wetGain);
        wetGain.connect(gainNode);
        
        let noteTimeouts = [];
        let chordIndex = 0;
        
        // Play a piano note with realistic harmonics
        const playPianoNote = (freq, velocity = 0.3, startTime = null) => {
            const now = startTime || this.audioContext.currentTime;
            
            // Fundamental
            const osc1 = this.audioContext.createOscillator();
            osc1.frequency.value = freq;
            osc1.type = 'sine';
            
            // 2nd harmonic (octave)
            const osc2 = this.audioContext.createOscillator();
            osc2.frequency.value = freq * 2;
            osc2.type = 'sine';
            
            // 3rd harmonic (fifth)
            const osc3 = this.audioContext.createOscillator();
            osc3.frequency.value = freq * 3;
            osc3.type = 'sine';
            
            // 4th harmonic
            const osc4 = this.audioContext.createOscillator();
            osc4.frequency.value = freq * 4;
            osc4.type = 'sine';
            
            // Envelope for fundamental
            const env1 = this.audioContext.createGain();
            env1.gain.setValueAtTime(0, now);
            env1.gain.linearRampToValueAtTime(velocity, now + 0.008);
            env1.gain.setValueAtTime(velocity * 0.8, now + 0.1);
            env1.gain.exponentialRampToValueAtTime(0.001, now + 6);
            
            // Harmonics decay faster
            const env2 = this.audioContext.createGain();
            env2.gain.setValueAtTime(velocity * 0.25, now);
            env2.gain.exponentialRampToValueAtTime(0.001, now + 4);
            
            const env3 = this.audioContext.createGain();
            env3.gain.setValueAtTime(velocity * 0.1, now);
            env3.gain.exponentialRampToValueAtTime(0.001, now + 2.5);
            
            const env4 = this.audioContext.createGain();
            env4.gain.setValueAtTime(velocity * 0.05, now);
            env4.gain.exponentialRampToValueAtTime(0.001, now + 1.5);
            
            osc1.connect(env1);
            osc2.connect(env2);
            osc3.connect(env3);
            osc4.connect(env4);
            
            env1.connect(dryGain);
            env1.connect(convolver);
            env2.connect(dryGain);
            env3.connect(dryGain);
            env4.connect(dryGain);
            
            osc1.start(now);
            osc2.start(now);
            osc3.start(now);
            osc4.start(now);
            
            osc1.stop(now + 6.5);
            osc2.stop(now + 4.5);
            osc3.stop(now + 3);
            osc4.stop(now + 2);
        };
        
        // Play a full chord with slight arpeggiation
        const playChord = () => {
            if (!this.activeSounds.has('Piano')) return;
            
            const chord = chordProgressions[chordIndex];
            chordIndex = (chordIndex + 1) % chordProgressions.length;
            
            const now = this.audioContext.currentTime;
            
            // Arpeggiate chord slightly for natural feel
            chord.forEach((freq, i) => {
                const delay = i * 0.03 + Math.random() * 0.02;
                const velocity = 0.25 - i * 0.02;
                playPianoNote(freq, velocity, now + delay);
            });
            
            // Schedule next chord
            noteTimeouts.push(setTimeout(playChord, 4000 + Math.random() * 2000));
        };
        
        // Play melodic embellishment
        const playMelody = () => {
            if (!this.activeSounds.has('Piano')) return;
            
            // Sometimes play 1-3 melody notes
            const noteCount = 1 + Math.floor(Math.random() * 3);
            const now = this.audioContext.currentTime;
            
            for (let i = 0; i < noteCount; i++) {
                const freq = melodyNotes[Math.floor(Math.random() * melodyNotes.length)];
                const delay = i * (0.3 + Math.random() * 0.4);
                playPianoNote(freq, 0.15 + Math.random() * 0.1, now + delay);
            }
            
            noteTimeouts.push(setTimeout(playMelody, 6000 + Math.random() * 8000));
        };
        
        // Start playing
        noteTimeouts.push(setTimeout(playChord, 300));
        noteTimeouts.push(setTimeout(playMelody, 2000));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => noteTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ STRINGS ============
    createStrings(volume = 0.5) {
        const notes = [196, 247, 294, 392];
        const oscillators = [];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.1;
        gainNode.connect(this.masterGain);
        
        const filter = this.audioContext.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.value = 1200;
        filter.connect(gainNode);
        
        notes.forEach((freq, i) => {
            for (let d = 0; d < 3; d++) {
                const osc = this.audioContext.createOscillator();
                osc.frequency.value = freq;
                osc.detune.value = (d - 1) * 8;
                osc.type = 'sawtooth';
                
                // Vibrato
                const lfo = this.audioContext.createOscillator();
                lfo.frequency.value = 4 + Math.random();
                const lfoGain = this.audioContext.createGain();
                lfoGain.gain.value = 2;
                lfo.connect(lfoGain);
                lfoGain.connect(osc.frequency);
                
                const oscGain = this.audioContext.createGain();
                oscGain.gain.value = 0.05 / (i + 1);
                
                osc.connect(oscGain);
                oscGain.connect(filter);
                osc.start();
                lfo.start();
                oscillators.push(osc, lfo);
            }
        });
        
        return { source: oscillators, gain: gainNode, type: 'generated' };
    }

    // ============ GUITAR - Fingerpicking with chords and arpeggios ============
    createGuitar(volume = 0.5) {
        // Guitar chord shapes (frequencies for common chords)
        const chords = {
            // G major
            G: [98.00, 123.47, 146.83, 196.00, 246.94, 392.00],
            // C major
            C: [130.81, 164.81, 196.00, 261.63, 329.63],
            // D major
            D: [146.83, 220.00, 293.66, 369.99],
            // Em
            Em: [82.41, 123.47, 164.81, 196.00, 246.94, 329.63],
            // Am
            Am: [110.00, 164.81, 220.00, 261.63, 329.63],
            // F major (partial)
            F: [174.61, 220.00, 261.63, 349.23]
        };
        
        const chordNames = ['G', 'C', 'D', 'Em', 'Am', 'G', 'C', 'D'];
        
        // Melodic notes for picking
        const melodyNotes = [329.63, 392.00, 440.00, 493.88, 523.25, 587.33, 659.25];
        
        const gainNode = this.audioContext.createGain();
        gainNode.gain.value = volume * 0.2;
        gainNode.connect(this.masterGain);
        
        // Add subtle reverb
        const sampleRate = this.audioContext.sampleRate;
        const convolver = this.audioContext.createConvolver();
        const reverbTime = 1.5;
        const reverbBuffer = this.audioContext.createBuffer(2, reverbTime * sampleRate, sampleRate);
        for (let ch = 0; ch < 2; ch++) {
            const data = reverbBuffer.getChannelData(ch);
            for (let i = 0; i < data.length; i++) {
                data[i] = (Math.random() * 2 - 1) * Math.exp(-4 * i / data.length);
            }
        }
        convolver.buffer = reverbBuffer;
        
        const dryGain = this.audioContext.createGain();
        dryGain.gain.value = 0.75;
        dryGain.connect(gainNode);
        
        const wetGain = this.audioContext.createGain();
        wetGain.gain.value = 0.25;
        convolver.connect(wetGain);
        wetGain.connect(gainNode);
        
        let noteTimeouts = [];
        let chordIndex = 0;
        
        // Play a single guitar note with realistic harmonics
        const playGuitarNote = (freq, velocity = 0.25, startTime = null) => {
            const now = startTime || this.audioContext.currentTime;
            
            // Fundamental (triangle for guitar-like tone)
            const osc1 = this.audioContext.createOscillator();
            osc1.frequency.value = freq;
            osc1.type = 'triangle';
            
            // 2nd harmonic
            const osc2 = this.audioContext.createOscillator();
            osc2.frequency.value = freq * 2;
            osc2.type = 'sine';
            
            // 3rd harmonic (adds brightness)
            const osc3 = this.audioContext.createOscillator();
            osc3.frequency.value = freq * 3;
            osc3.type = 'sine';
            
            // Pluck envelope - fast attack, medium decay
            const env1 = this.audioContext.createGain();
            env1.gain.setValueAtTime(0, now);
            env1.gain.linearRampToValueAtTime(velocity, now + 0.005);
            env1.gain.setValueAtTime(velocity * 0.7, now + 0.05);
            env1.gain.exponentialRampToValueAtTime(0.001, now + 3);
            
            const env2 = this.audioContext.createGain();
            env2.gain.setValueAtTime(velocity * 0.3, now);
            env2.gain.exponentialRampToValueAtTime(0.001, now + 2);
            
            const env3 = this.audioContext.createGain();
            env3.gain.setValueAtTime(velocity * 0.15, now);
            env3.gain.exponentialRampToValueAtTime(0.001, now + 1);
            
            // Slight filter sweep for realism
            const filter = this.audioContext.createBiquadFilter();
            filter.type = 'lowpass';
            filter.frequency.setValueAtTime(3000, now);
            filter.frequency.exponentialRampToValueAtTime(800, now + 2);
            
            osc1.connect(env1);
            osc2.connect(env2);
            osc3.connect(env3);
            env1.connect(filter);
            env2.connect(filter);
            env3.connect(filter);
            filter.connect(dryGain);
            filter.connect(convolver);
            
            osc1.start(now);
            osc2.start(now);
            osc3.start(now);
            osc1.stop(now + 3.5);
            osc2.stop(now + 2.5);
            osc3.stop(now + 1.5);
        };
        
        // Play an arpeggiated chord (fingerpicking style)
        const playArpeggio = () => {
            if (!this.activeSounds.has('Guitar')) return;
            
            const chordName = chordNames[chordIndex % chordNames.length];
            const chord = chords[chordName];
            chordIndex++;
            
            const now = this.audioContext.currentTime;
            const pattern = Math.floor(Math.random() * 3);
            
            if (pattern === 0) {
                // Ascending arpeggio
                chord.forEach((freq, i) => {
                    const delay = i * 0.12;
                    const velocity = 0.2 - i * 0.02;
                    playGuitarNote(freq, velocity, now + delay);
                });
            } else if (pattern === 1) {
                // Travis picking pattern (bass, high, mid, high)
                const bassNote = chord[0];
                const midNote = chord[Math.floor(chord.length / 2)];
                const highNote = chord[chord.length - 1];
                
                playGuitarNote(bassNote, 0.25, now);
                playGuitarNote(highNote, 0.15, now + 0.15);
                playGuitarNote(midNote, 0.18, now + 0.3);
                playGuitarNote(highNote, 0.15, now + 0.45);
                playGuitarNote(bassNote, 0.22, now + 0.6);
                playGuitarNote(highNote, 0.15, now + 0.75);
            } else {
                // Strum (all notes close together)
                chord.forEach((freq, i) => {
                    const delay = i * 0.025;
                    playGuitarNote(freq, 0.18, now + delay);
                });
            }
            
            noteTimeouts.push(setTimeout(playArpeggio, 2500 + Math.random() * 2000));
        };
        
        // Play melodic fills between chords
        const playMelody = () => {
            if (!this.activeSounds.has('Guitar')) return;
            
            const now = this.audioContext.currentTime;
            const noteCount = 2 + Math.floor(Math.random() * 3);
            
            for (let i = 0; i < noteCount; i++) {
                const freq = melodyNotes[Math.floor(Math.random() * melodyNotes.length)];
                const delay = i * (0.2 + Math.random() * 0.3);
                playGuitarNote(freq, 0.12 + Math.random() * 0.08, now + delay);
            }
            
            noteTimeouts.push(setTimeout(playMelody, 5000 + Math.random() * 6000));
        };
        
        // Start playing
        noteTimeouts.push(setTimeout(playArpeggio, 300));
        noteTimeouts.push(setTimeout(playMelody, 3000));
        
        return {
            gain: gainNode,
            type: 'generated',
            cleanup: () => noteTimeouts.forEach(t => clearTimeout(t))
        };
    }

    // ============ SOUND MANAGEMENT ============

    playSound(soundName, volume = 70) {
        this.resume();
        
        if (this.activeSounds.has(soundName)) return;
        
        const vol = volume / 100;
        let soundObj;
        
        switch (soundName) {
            case 'Rain': soundObj = this.createRain(vol); break;
            case 'Ocean Waves': soundObj = this.createOceanWaves(vol); break;
            case 'Forest': soundObj = this.createForest(vol); break;
            case 'Thunder': soundObj = this.createThunder(vol); break;
            case 'Wind': soundObj = this.createWind(vol); break;
            case 'Waterfall': soundObj = this.createWaterfall(vol); break;
            case 'Fireplace': soundObj = this.createFireplace(vol); break;
            case 'Coffee Shop': soundObj = this.createCoffeeShop(vol); break;
            case 'White Noise': soundObj = this.createWhiteNoise(vol); break;
            case 'Pink Noise': soundObj = this.createPinkNoise(vol); break;
            case 'Brown Noise': soundObj = this.createBrownNoise(vol); break;
            case 'Binaural Beats': soundObj = this.createBinauralBeats(vol); break;
            case 'Singing Bowls': soundObj = this.createSingingBowl(vol); break;
            case 'Chimes': soundObj = this.createChimes(vol); break;
            case 'Temple Bells': soundObj = this.createTempleBells(vol); break;
            case 'Om Chant': soundObj = this.createOmChant(vol); break;
            case 'Ambient Pads': soundObj = this.createAmbientPad(vol); break;
            case 'Piano': soundObj = this.createPiano(vol); break;
            case 'Strings': soundObj = this.createStrings(vol); break;
            case 'Guitar': soundObj = this.createGuitar(vol); break;
            default: return;
        }
        
        if (soundObj) {
            this.activeSounds.set(soundName, soundObj);
        }
    }

    stopSound(soundName) {
        const soundObj = this.activeSounds.get(soundName);
        if (!soundObj) return;
        
        try {
            if (soundObj.cleanup) soundObj.cleanup();
            
            if (Array.isArray(soundObj.source)) {
                soundObj.source.forEach(s => { try { s.stop(); } catch(e) {} });
            } else if (soundObj.source) {
                try { soundObj.source.stop(); } catch(e) {}
            }
            
            if (soundObj.gain) soundObj.gain.disconnect();
        } catch (e) {}
        
        this.activeSounds.delete(soundName);
    }

    updateVolume(soundName, volume) {
        const soundObj = this.activeSounds.get(soundName);
        if (soundObj && soundObj.gain) {
            soundObj.gain.gain.value = (volume / 100) * 0.5;
        }
    }

    stopAllSounds() {
        this.activeSounds.forEach((_, name) => this.stopSound(name));
    }

    pauseAll() {
        if (this.audioContext) this.audioContext.suspend();
    }

    resumeAll() {
        if (this.audioContext) this.audioContext.resume();
    }
}

// Global instance
window.soundTherapyManager = new SoundTherapyManager();

// Blazor interop
window.initializeSoundTherapy = async () => {
    await window.soundTherapyManager.initialize();
    return true;
};

window.playTherapySound = (name, vol) => window.soundTherapyManager.playSound(name, vol);
window.stopTherapySound = (name) => window.soundTherapyManager.stopSound(name);
window.updateSoundVolume = (name, vol) => window.soundTherapyManager.updateVolume(name, vol);
window.stopAllTherapySounds = () => window.soundTherapyManager.stopAllSounds();
window.pauseTherapySounds = () => window.soundTherapyManager.pauseAll();
window.resumeTherapySounds = () => window.soundTherapyManager.resumeAll();
