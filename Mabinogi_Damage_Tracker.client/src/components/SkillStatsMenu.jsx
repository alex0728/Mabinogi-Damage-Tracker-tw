import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Divider from '@mui/material/Divider';
import Grid from '@mui/material/Grid';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Collapse from '@mui/material/Collapse';
import IconButton from '@mui/material/IconButton';
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp';
import skillIdToName from '../skill_id_to_tw_name.json';

function getSkillName(skillId) {
    return skillIdToName[String(skillId)] || `技能 ${skillId}`;
}

function getSkillIconUrl(skillId) {
    return `/res/skillimage/kr/${skillId}/${skillId}.png`;
}

function formatLargeNumber(num) {
    if (num === null || num === undefined || isNaN(num)) return '0';
    const absNum = Math.abs(num);
    if (absNum >= 1e12) return (num / 1e12).toFixed(1) + 'T';
    if (absNum >= 1e9) return (num / 1e9).toFixed(1) + 'B';
    if (absNum >= 1e6) return (num / 1e6).toFixed(1) + 'M';
    if (absNum >= 1e3) return (num / 1e3).toFixed(1) + 'K';
    return num.toFixed(0);
}

function formatDuration(seconds, t) {
    if (!seconds || seconds === 0) return t ? t('common.seconds', { count: 0 }) : '0s';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    if (hours > 0) return t ? t('common.hours', { h: hours, m: minutes }) : `${hours}h ${minutes}m`;
    if (minutes > 0) return t ? t('common.minutes', { m: minutes, s: secs }) : `${minutes}m ${secs}s`;
    return t ? t('common.seconds', { count: secs }) : `${secs}s`;
}

function formatPlayerId(id) {
    if (id === null || id === undefined) return 'Unknown';
    // Use decimal format like other pages
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

export default function SkillStatsMenu({ start_ut, end_ut }) {
    const { t } = useTranslation();
    const [skillStats, setSkillStats] = useState([]);
    const [combatStats, setCombatStats] = useState(null);
    const [playerDps, setPlayerDps] = useState([]);
    const [selectedPlayer, setSelectedPlayer] = useState(null);
    const [openPlayers, setOpenPlayers] = useState({});
    const [loading, setLoading] = useState(true);
    const [playerSkills, setPlayerSkills] = useState({});

    useEffect(() => {
        async function fetchData() {
            setLoading(true);
            try {
                const dpsResponse = await fetch(
                    `http://${window.location.hostname}:5004/Home/GetPlayerDps?start_ut=${start_ut}&end_ut=${end_ut}`
                );
                const dpsData = await dpsResponse.json();
                setPlayerDps(dpsData || []);

                // Fetch all skills (no player filter)
                const skillResponse = await fetch(
                    `http://${window.location.hostname}:5004/Home/GetSkillStats?start_ut=${start_ut}&end_ut=${end_ut}`
                );
                const skillData = await skillResponse.json();
                
                const totalDamage = (skillData || []).reduce((sum, s) => sum + s.total_damage, 0);
                
                const processedData = (skillData || []).map(s => ({
                    ...s,
                    damage_distribution: totalDamage > 0 ? (s.total_damage / totalDamage) * 100 : 0
                })).sort((a, b) => b.total_damage - a.total_damage);
                
                setSkillStats(processedData);

                const combatResponse = await fetch(
                    `http://${window.location.hostname}:5004/Home/GetCombatStats?start_ut=${start_ut}&end_ut=${end_ut}`
                );
                const combatData = await combatResponse.json();
                setCombatStats(combatData);
            } catch (error) {
                console.error('Error fetching skill stats:', error);
            } finally {
                setLoading(false);
            }
        }

        if (start_ut && end_ut) {
            fetchData();
        }
    }, [start_ut, end_ut]);

    const fetchPlayerSkills = async (playerId) => {
        if (playerSkills[playerId]) return; // Already fetched
        
        try {
            const response = await fetch(
                `http://${window.location.hostname}:5004/Home/GetSkillStats?start_ut=${start_ut}&end_ut=${end_ut}&playerid=${playerId}`
            );
            const data = await response.json();
            
            const totalDamage = (data || []).reduce((sum, s) => sum + s.total_damage, 0);
            const processed = (data || []).map(s => ({
                ...s,
                damage_distribution: totalDamage > 0 ? (s.total_damage / totalDamage) * 100 : 0
            })).sort((a, b) => b.total_damage - a.total_damage);
            
            setPlayerSkills(prev => ({ ...prev, [playerId]: processed }));
        } catch (error) {
            console.error('Error fetching player skills:', error);
        }
    };

    const handlePlayerClick = (playerId) => {
        const newOpen = { ...openPlayers };
        if (newOpen[playerId]) {
            delete newOpen[playerId];
        } else {
            newOpen[playerId] = true;
            fetchPlayerSkills(playerId);
        }
        setOpenPlayers(newOpen);
    };

    if (loading) {
        return (
            <Box sx={{ width: '100%', padding: '16px' }}>
                <Skeleton variant="rectangular" height={200} sx={{ mb: 2 }} />
                <Skeleton variant="rectangular" height={400} />
            </Box>
        );
    }

    return (
        <Box sx={{ width: '100%', padding: '16px' }}>
            {/* Combat Stats */}
            <Grid container spacing={2} sx={{ mb: 3 }}>
                <Grid item xs={12} md={4}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="h6" color="primary" gutterBottom>戰鬥次數</Typography>
                        <Typography variant="h3">{combatStats?.combat_count || 0}</Typography>
                    </Paper>
                </Grid>
                <Grid item xs={12} md={4}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="h6" color="primary" gutterBottom>總戰鬥時長</Typography>
                        <Typography variant="h3">{formatDuration(combatStats?.total_duration_seconds || 0, t)}</Typography>
                    </Paper>
                </Grid>
                <Grid item xs={12} md={4}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="h6" color="primary" gutterBottom>平均戰鬥時長</Typography>
                        <Typography variant="h3">{formatDuration(combatStats?.avg_combat_duration_seconds || 0, t)}</Typography>
                    </Paper>
                </Grid>
            </Grid>

            <Divider sx={{ my: 3 }} />

            {/* Player DPS Stats - Collapsible */}
            <Typography variant="h5" gutterBottom>{t('skillStats.playerDpsStats')}</Typography>
            
            {playerDps.length === 0 ? (
                <Paper sx={{ p: 4, textAlign: 'center' }}>
                    <Typography variant="body1" color="text.secondary">{t('skillStats.noPlayerData')}</Typography>
                </Paper>
            ) : (
                <TableContainer component={Paper}>
                    <Table>
                        <TableHead>
                            <TableRow sx={{ backgroundColor: 'primary.main' }}>
                                <TableCell sx={{ color: 'white', width: 50 }}></TableCell>
                                <TableCell sx={{ color: 'white', fontWeight: 'bold' }}>{t('skillStats.playerName')}</TableCell>
                                <TableCell sx={{ color: 'white', fontWeight: 'bold' }} align="right">{t('skillStats.totalDps')}</TableCell>
                                <TableCell sx={{ color: 'white', fontWeight: 'bold' }} align="right">{t('skillStats.damagePercent')}</TableCell>
                                <TableCell sx={{ color: 'white', fontWeight: 'bold' }} align="right">{t('skillStats.dps')}</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {playerDps.map((player) => {
                                const duration = combatStats?.total_duration_seconds || 1;
                                const dps = player.total_damage / duration;
                                const isOpen = openPlayers[player.player_id];
                                const skills = playerSkills[player.player_id] || [];
                                
                                return (
                                    <>
                                        <TableRow 
                                            key={player.player_id} 
                                            onClick={() => handlePlayerClick(player.player_id)}
                                            sx={{ cursor: 'pointer', '&:hover': { backgroundColor: 'action.hover' } }}
                                        >
                                            <TableCell>
                                                <IconButton size="small">
                                                    {isOpen ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
                                                </IconButton>
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body1" fontWeight="medium">
                                                    {formatPlayerDisplay(player)}
                                                </Typography>
                                            </TableCell>
                                            <TableCell align="right">
                                                <Typography variant="body1" color="primary">{formatLargeNumber(dps)}</Typography>
                                            </TableCell>
                                            <TableCell align="right">
                                                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 1 }}>
                                                    <Box sx={{ width: 80, height: 8, bgcolor: 'grey.200', borderRadius: 1, overflow: 'hidden' }}>
                                                        <Box sx={{ width: `${Math.min(100, player.dps_percentage)}%`, height: '100%', bgcolor: 'primary.main' }} />
                                                    </Box>
                                                    <Typography variant="body2">{player.dps_percentage.toFixed(1)}%</Typography>
                                                </Box>
                                            </TableCell>
                                            <TableCell align="right">
                                                <Typography variant="body1">{formatLargeNumber(player.total_damage)}</Typography>
                                            </TableCell>
                                        </TableRow>
                                        <TableRow>
                                            <TableCell colSpan={5} sx={{ py: 0, borderBottom: isOpen ? 1 : 0 }}>
                                                <Collapse in={isOpen} timeout="auto" unmountOnExit>
                                                    <Box sx={{ py: 2 }}>
                                                        <Typography variant="subtitle2" gutterBottom>{t('skillStats.title')}</Typography>
                                                        {skills.length === 0 ? (
                                                            <Typography variant="body2" color="text.secondary">載入中...</Typography>
                                                        ) : (
                                                            <Table size="small">
                                                                <TableHead>
                                                                    <TableRow sx={{ backgroundColor: 'grey.100' }}>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>{t('skillStats.skill')}</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }} align="right">使用次數</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }} align="right">{t('skillStats.totalDamage')}</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }} align="right">平均</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }} align="right">最小</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }} align="right">最大</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>佔比</TableCell>
                                                                    </TableRow>
                                                                </TableHead>
                                                                <TableBody>
                                                                    {skills.map((skill) => (
                                                                        <TableRow key={skill.skill_id}>
                                                                            <TableCell>
                                                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                                                    <img src={getSkillIconUrl(skill.skill_id)} alt="" style={{ width: 24, height: 24 }} onError={(e) => e.target.style.display = 'none'} />
                                                                                    <Typography variant="body2">{getSkillName(skill.skill_id)}</Typography>
                                                                                </Box>
                                                                            </TableCell>
                                                                            <TableCell align="right">{skill.hit_count}</TableCell>
                                                                            <TableCell align="right">{formatLargeNumber(skill.total_damage)}</TableCell>
                                                                            <TableCell align="right">{formatLargeNumber(skill.avg_damage)}</TableCell>
                                                                            <TableCell align="right">{formatLargeNumber(skill.min_damage)}</TableCell>
                                                                            <TableCell align="right">{formatLargeNumber(skill.max_damage)}</TableCell>
                                                                            <TableCell>{skill.damage_distribution.toFixed(1)}%</TableCell>
                                                                        </TableRow>
                                                                    ))}
                                                                </TableBody>
                                                            </Table>
                                                        )}
                                                    </Box>
                                                </Collapse>
                                            </TableCell>
                                        </TableRow>
                                    </>
                                );
                            })}
                        </TableBody>
                    </Table>
                </TableContainer>
            )}
        </Box>
    );
}
