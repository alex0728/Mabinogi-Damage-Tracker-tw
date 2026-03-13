import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './en.json';
import ja from './ja.json';
import zhTW from './zh-TW.json';

const getInitialLanguage = () => {
  const savedLanguage = localStorage.getItem('lang');
  if (savedLanguage) {
    return savedLanguage;
  }

  // Default to Traditional Chinese
  return 'zh-TW';
};

if (!i18n.isInitialized) {
  i18n
    .use(initReactI18next)
    .init({
      resources: {
        en: { translation: en },
        ja: { translation: ja },
        'zh-TW': { translation: zhTW },
      },
      lng: getInitialLanguage(),
      fallbackLng: 'en',
      interpolation: {
        escapeValue: false,
      },
    });
}

export default i18n;
