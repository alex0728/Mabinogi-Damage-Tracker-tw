import * as React from 'react';
import { useState, useEffect, useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography'
import AutoAwesomeIcon from '@mui/icons-material/AutoAwesome';
import DotsMobileStepper from './DotsMobileStepper';
import { useTranslation } from 'react-i18next';

function formatLargeNumber(num) {
    if (num === null || num === undefined || isNaN(num)) return '0';

    const absNum = Math.abs(num);
    let formatted;

    if (absNum >= 1e12) {
        formatted = (num / 1e12).toFixed(1) + 'T';
    } else if (absNum >= 1e9) {
        formatted = (num / 1e9).toFixed(1) + 'B';
    } else if (absNum >= 1e6) {
        formatted = (num / 1e6).toFixed(1) + 'M';
    } else if (absNum >= 1e3) {
        formatted = (num / 1e3).toFixed(1) + 'K';
    } else {
        formatted = num.toFixed(0);
    }

    return formatted.replace(/\.0(?=[A-Z])/, '');
}

function formatPlayerId(id) {
    if (!id) return 'Unknown';
    return id.toString();
}

function formatPlayerDisplay(player) {
    if (!player) return 'Unknown';
    const name = player.player_name;
    if (name && typeof name === 'string' && name.trim() !== '') {
        return name.trim();
    }
    if (player.player_id) {
        return formatPlayerId(player.player_id);
    }
    return 'Unknown';
}

export default function BurstCard({ bands, graphBands, setGraphBands }) {
    const { t } = useTranslation();
    const [activeStep, setActiveStep] = useState(0);
    const prevBandsRef = useRef(null);
    
    // Guard against empty or invalid bands - but call hooks first
    const isValid = bands && Array.isArray(bands) && bands.length > 0;
    const cardLabel = isValid ? (bands[0]?.label || '') : '';
    const currentBurst = isValid ? bands[activeStep] : null;

    useEffect(() => {
        if (!isValid || !setGraphBands) return;
        
        // Prevent infinite loop by checking if bands actually changed
        const bandsJson = JSON.stringify(bands);
        if (prevBandsRef.current === bandsJson) return;
        prevBandsRef.current = bandsJson;
        
        // Use functional update to avoid dependency on graphBands
        setGraphBands(prev => {
            if (!prev || !Array.isArray(prev)) return prev;
            const newBands = prev.map(band =>
                band.label === cardLabel ? bands[activeStep] : band
            );
            // Only update if something actually changed
            if (JSON.stringify(prev) === JSON.stringify(newBands)) return prev;
            return newBands;
        });
    }, [activeStep, cardLabel, isValid, setGraphBands]) // Removed bands from dependencies

    if (!isValid || !currentBurst) {
        return null;
    }

    return (
        <Paper square={false} sx={{ position: 'relative', "padding-left": "32px","padding-top":"20px", gap: "10px", height: "100%", display: 'flex', flexDirection: 'column'}}>
            <AutoAwesomeIcon fontSize="medium" />
            <Box sx={{ display: "flex", flexDirection: { xs: 'column', md: 'row' }, gap: { xs: 2, sm: 4, md: 8 }}}>
                <Box sx={{ gap: "5px", flexGrow: "2"}} >
                    <Typography variant="subtitle1">{t('analytics.largestBurst', { label: currentBurst.label })}</Typography>
                    <Typography variant="h3">{formatPlayerDisplay(currentBurst)}</Typography>
                    <Typography variant="h3">{formatLargeNumber(currentBurst.damage)}</Typography>
                    <Typography variant="subtitle1">{t('analytics.startedAt', { time: currentBurst?.start || '-' })}</Typography>
                </Box>
            </Box>
            <Box sx={{ position: 'absolute', bottom: 25 , left: '50%', transform: "translate(-50%, 50%)" }} >
                <DotsMobileStepper steps={bands.length} activeStep={activeStep} setActiveStep={setActiveStep}/>
            </Box>
        </Paper>
    );
}
