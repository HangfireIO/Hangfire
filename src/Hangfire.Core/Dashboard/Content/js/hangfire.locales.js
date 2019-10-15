
function i18n(template) {
    let db = i18n.db[i18n.locale] || i18n.db['en'];
    for (var
        info = db[template.join('\x01')],
        out = [info.t[0]],
        i = 1, length = info.t.length; i < length; i++
    ) out[i] = arguments[1 + info.v[i - 1]] + info.t[i];
    return out.join('');
}
i18n.locale = 'en';
i18n.db = {};

i18n.set = locale => (tCurrent, ...rCurrent) => {
    const key = tCurrent.join('\x01');
    let db = i18n.db[locale] || (i18n.db[locale] = {});
    db[key] = {
        t: tCurrent.slice(),
        v: rCurrent.map((value, i) => i)
    };
    const config = {
        for: other => (tOther, ...rOther) => {
            db = i18n.db[other] || (i18n.db[other] = {});
            db[key] = {
                t: tOther.slice(),
                v: rOther.map((value, i) => rCurrent.indexOf(value))
            };
            return config;
        }
    };
    return config;
};

i18n.set('en')`More details...`
    .for('pt')`Mais detalhes...`
    .for('pt-BR')`Mais detalhes...`;

i18n.set('en')`Fewer details...`
    .for('pt')`Menos detalhes...`
    .for('pt-BR')`Menos detalhes...`;